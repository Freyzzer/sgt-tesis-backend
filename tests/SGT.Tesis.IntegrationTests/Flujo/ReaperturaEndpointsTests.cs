using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Flujo;

/// <summary>Reapertura de una etapa rechazada por el administrador (ADR-005).</summary>
public class ReaperturaEndpointsTests
{
    private const string Tesis = "/api/v1/tesis";

    private sealed record Escenario(ApiFactory Factory, Guid TesisId, string CorreoEstudiante, HttpClient Estudiante, HttpClient Admin);

    private static string Etapa(Guid tesisId) => $"{Tesis}/{tesisId}/etapas/1";

    /// <summary>Tesis con la etapa 1 rechazada. Con <paramref name="definitivo"/> la política cierra la tesis.</summary>
    private static async Task<Escenario> RechazadaAsync(ApiFactory factory, bool definitivo)
    {
        if (definitivo)
        {
            factory.Configuracion["Flujo:Etapas:Estudiante:PermiteReintento"] = "false";
        }

        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();
        await estudiante.PostAsync($"{Etapa(tesisId)}/enviar", null);
        var admin = await factory.ClienteAdministradorAsync();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Etapa(tesisId)}/decisiones", new { resultado = "Rechazada", justificacion = "No cumple." })).StatusCode);

        return new Escenario(factory, tesisId, correo, estudiante, admin);
    }

    private static Task<HttpResponseMessage> Reabrir(Escenario e, string? justificacion = "Error en la decisión.")
    {
        // En la realidad cada acción ocurre en un instante distinto; el reloj de prueba es fijo y SQL Server desempata los
        // GUID distinto que SQLite, así que se avanza para que el orden de las decisiones no dependa del motor.
        e.Factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));

        return e.Admin.PostAsJsonAsync($"{Etapa(e.TesisId)}/reabrir", new { justificacion });
    }

    [Fact]
    public async Task Reabrir_un_rechazo_definitivo_devuelve_la_tesis_al_estudiante_que_puede_corregir_y_reenviar()
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: true);
        Assert.Equal("Rechazada", (await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("estado").GetString());

        var respuesta = await Reabrir(e);
        var tesis = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("EnCurso", tesis.GetProperty("estado").GetString());
        Assert.Equal("Observado", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal("Reabierta", tesis.GetProperty("decisiones")[1].GetProperty("resultado").GetString());
        Assert.Equal("Error en la decisión.", tesis.GetProperty("decisiones")[1].GetProperty("justificacion").GetString());

        var reenvio = await (await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnRevision", reenvio.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal(2, reenvio.GetProperty("etapaActual").GetProperty("ronda").GetInt32());
    }

    [Fact]
    public async Task Solo_el_administrador_reabre()
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: true);
        var (_, correoEv) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await factory.ClienteComoAsync(correoEv);
        using var anonimo = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await e.Estudiante.PostAsJsonAsync($"{Etapa(e.TesisId)}/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.PostAsJsonAsync($"{Etapa(e.TesisId)}/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync($"{Etapa(e.TesisId)}/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task La_justificacion_es_obligatoria(string? justificacion)
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: true);

        Assert.Equal(HttpStatusCode.BadRequest, (await Reabrir(e, justificacion)).StatusCode);
    }

    [Fact]
    public async Task Solo_se_reabre_una_etapa_rechazada_y_decidir_no_acepta_Reabierta()
    {
        using var factory = new ApiFactory();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" }, cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        await estudiante.PostAsync($"{Etapa(tesisId)}/enviar", null, TestContext.Current.CancellationToken);
        using var admin = await factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Etapa(tesisId)}/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Etapa(tesisId)}/decisiones", new { resultado = "Reabierta", justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"{Tesis}/{Guid.NewGuid()}/etapas/1/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Etapa(tesisId).Replace("etapas/1", "etapas/9")}/reabrir", new { justificacion = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task No_se_reabre_un_rechazo_definitivo_si_el_estudiante_ya_registro_otra_tesis()
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: true);
        Assert.Equal(HttpStatusCode.Created, (await e.Estudiante.PostAsJsonAsync(Tesis, new { titulo = "Nueva", resumen = "Otro intento" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var respuesta = await Reabrir(e);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("otra tesis en curso", await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_reapertura_avisa_al_estudiante_queda_auditada_y_se_ve_en_la_retroalimentacion()
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: false);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
        }

        var antes = factory.Correos.Enviados.Count;

        Assert.Equal(HttpStatusCode.OK, (await Reabrir(e, "Se concede otra oportunidad.")).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.RegistrosAuditoria.AnyAsync(r => r.Accion == AccionesAuditoria.EtapaReabierta && r.TesisId == e.TesisId, TestContext.Current.CancellationToken));
        }

        var correo = Assert.Single(factory.Correos.Enviados.Skip(antes));
        Assert.Equal(e.CorreoEstudiante, correo.Destinatario);
        Assert.Contains("reabierta", correo.Asunto, StringComparison.Ordinal);
        Assert.DoesNotContain("Se concede otra oportunidad.", correo.Cuerpo, StringComparison.Ordinal);

        var retro = await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}/retroalimentacion", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var decisiones = retro.GetProperty("etapas")[0].GetProperty("decisiones").EnumerateArray().Select(d => d.GetProperty("resultado").GetString()).ToList();
        Assert.Equal(["Rechazada", "Reabierta"], decisiones);
    }

    [Fact]
    public async Task Las_decisiones_reabiertas_no_alteran_los_indicadores_de_aprobacion()
    {
        using var factory = new ApiFactory();
        var e = await RechazadaAsync(factory, definitivo: true);
        var antes = await (await e.Admin.GetAsync("/api/v1/dashboard/kpis", TestContext.Current.CancellationToken)).LeerJsonAsync();

        await Reabrir(e);
        var despues = await (await e.Admin.GetAsync("/api/v1/dashboard/kpis", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(antes.GetProperty("decisiones").GetProperty("total").GetInt32(), despues.GetProperty("decisiones").GetProperty("total").GetInt32());
        Assert.Equal(1, despues.GetProperty("decisiones").GetProperty("rechazadas").GetInt32());
    }
}
