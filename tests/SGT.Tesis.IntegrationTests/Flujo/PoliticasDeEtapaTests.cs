using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure;
using SGT.Tesis.Infrastructure.Flujo;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Flujo;

/// <summary>Reglas por etapa (ADR-005) a través de la API y de la configuración.</summary>
public class PoliticasDeEtapaTests
{
    private const string Tesis = "/api/v1/tesis";

    private static async Task<(Guid TesisId, HttpClient Estudiante)> TesisEnRevisionAsync(ApiFactory factory)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var id = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Tesis}/{id}/etapas/1/enviar", null)).StatusCode);

        return (id, estudiante);
    }

    private static Task<HttpResponseMessage> Rechazar(HttpClient admin, Guid tesisId) =>
        admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/decisiones", new { resultado = "Rechazada", justificacion = "No cumple." });

    // ---------- Rechazo reintentable o definitivo ----------

    [Fact]
    public async Task Por_defecto_rechazar_la_etapa_1_permite_corregir_y_reenviar_sin_perder_la_tesis()
    {
        using var factory = new ApiFactory();
        var (tesisId, estudiante) = await TesisEnRevisionAsync(factory);
        using var admin = await factory.ClienteAdministradorAsync();

        var respuesta = await Rechazar(admin, tesisId);
        var tesis = await (await estudiante.GetAsync($"{Tesis}/{tesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("EnCurso", tesis.GetProperty("estado").GetString());
        Assert.Equal("Rechazado", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal(JsonValueKindNull, tesis.GetProperty("etapaActual").GetProperty("fechaCierre").ValueKind);

        var reenvio = await estudiante.PostAsync($"{Tesis}/{tesisId}/etapas/1/enviar", null, TestContext.Current.CancellationToken);
        var reenviada = await reenvio.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, reenvio.StatusCode);
        Assert.Equal("EnRevision", reenviada.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal("EnCurso", reenviada.GetProperty("estado").GetString());
    }

    private const System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;

    [Fact]
    public async Task Con_la_politica_en_definitivo_rechazar_cierra_la_tesis_y_no_admite_reenvio()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Flujo:Etapas:Estudiante:PermiteReintento"] = "false";
        var (tesisId, estudiante) = await TesisEnRevisionAsync(factory);
        using var admin = await factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.Created, (await Rechazar(admin, tesisId)).StatusCode);
        var tesis = await (await estudiante.GetAsync($"{Tesis}/{tesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal("Rechazada", tesis.GetProperty("estado").GetString());
        Assert.NotEqual(JsonValueKindNull, tesis.GetProperty("etapaActual").GetProperty("fechaCierre").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await estudiante.PostAsync($"{Tesis}/{tesisId}/etapas/1/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
    }

    private static async Task ProcesarCorreosAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task El_correo_de_un_rechazo_reintentable_invita_a_corregir_y_el_definitivo_no()
    {
        using var reintentable = new ApiFactory();
        var (t1, _) = await TesisEnRevisionAsync(reintentable);
        using var admin1 = await reintentable.ClienteAdministradorAsync();
        await Rechazar(admin1, t1);
        await ProcesarCorreosAsync(reintentable);

        using var definitiva = new ApiFactory();
        definitiva.Configuracion["Flujo:Etapas:Estudiante:PermiteReintento"] = "false";
        var (t2, _) = await TesisEnRevisionAsync(definitiva);
        using var admin2 = await definitiva.ClienteAdministradorAsync();
        await Rechazar(admin2, t2);
        await ProcesarCorreosAsync(definitiva);

        Assert.Contains(reintentable.Correos.Enviados, c => c.Cuerpo.Contains("volver a enviarla", StringComparison.Ordinal));
        Assert.DoesNotContain(definitiva.Correos.Enviados, c => c.Cuerpo.Contains("volver a enviarla", StringComparison.Ordinal));
        Assert.Contains(definitiva.Correos.Enviados, c => c.Asunto.Contains("rechazada", StringComparison.Ordinal));
    }

    // ---------- Tope de evaluadores ----------

    [Fact]
    public async Task No_se_asignan_mas_evaluadores_que_el_maximo_de_la_etapa_pero_se_puede_reemplazar_a_uno()
    {
        using var factory = new ApiFactory();
        var (tesisId, _) = await TesisEnRevisionAsync(factory);
        using var admin = await factory.ClienteAdministradorAsync();
        var ids = new List<Guid>();

        for (var i = 0; i < 4; i++)
        {
            ids.Add((await factory.SembrarUsuarioAsync(Rol.Evaluador)).Id);
        }

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores", new { evaluadorId = ids[i] }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        }

        var cuarto = await admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores", new { evaluadorId = ids[3] }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, cuarto.StatusCode);
        Assert.Contains("máximo de 3", await cuarto.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores/{ids[0]}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores", new { evaluadorId = ids[3] }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task El_maximo_se_puede_cambiar_por_configuracion()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Flujo:Etapas:Estudiante:MaxEvaluadores"] = "1";
        var (tesisId, _) = await TesisEnRevisionAsync(factory);
        using var admin = await factory.ClienteAdministradorAsync();
        var (a, _) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var (b, _) = await factory.SembrarUsuarioAsync(Rol.Evaluador);

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores", new { evaluadorId = a }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"{Tesis}/{tesisId}/etapas/1/evaluadores", new { evaluadorId = b }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- Configuración ----------

    private sealed class UsuarioAnonimo : ICurrentUser
    {
        public Guid? UserId => null;

        public string? Rol => null;
    }

    private static ServiceProvider Construir(Dictionary<string, string?> valores)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new UsuarioAnonimo());
        services.AddInfrastructure(configuracion);

        return services.BuildServiceProvider();
    }

    private static readonly Dictionary<string, string?> Minima = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(none);Database=pruebas;User ID=u;Password=marcador",
        ["Jwt:Issuer"] = "x",
        ["Jwt:Audience"] = "y",
        ["Jwt:SigningKey"] = "clave-de-pruebas-que-supera-los-32-bytes-0123456789",
        ["Storage:RootPath"] = "C:/tmp/sgt",
        ["Email:Modo"] = "Log",
    };

    [Fact]
    public void Sin_configuracion_cada_etapa_usa_los_valores_acordados()
    {
        using var provider = Construir(new(Minima));
        var politicas = provider.GetRequiredService<IPoliticasDeEtapa>();

        Assert.True(politicas.Para(EtapaTipo.Propuesta).PermiteReintento);
        Assert.True(politicas.Para(EtapaTipo.RequisitosFinales).PermiteReintento);
        Assert.False(politicas.Para(EtapaTipo.Pasantia).PermiteReintento);
        Assert.False(politicas.Para(EtapaTipo.Articulo).PermiteReintento);
        Assert.All(Enum.GetValues<EtapaTipo>(), t => Assert.Equal(3, politicas.Para(t).MaxEvaluadores));
    }

    [Fact]
    public void La_configuracion_cambia_solo_lo_indicado_y_conserva_el_resto()
    {
        using var provider = Construir(new(Minima)
        {
            ["Flujo:Etapas:Pasantia:PermiteReintento"] = "true",
            ["Flujo:Etapas:Consejo:ExigeUnanimidad"] = "false",
            ["Flujo:Etapas:Consejo:MaxEvaluadores"] = "2",
        });
        var politicas = provider.GetRequiredService<IPoliticasDeEtapa>();

        Assert.True(politicas.Para(EtapaTipo.Pasantia).PermiteReintento);
        Assert.Equal(3, politicas.Para(EtapaTipo.Pasantia).MaxEvaluadores);
        Assert.False(politicas.Para(EtapaTipo.Consejo).ExigeUnanimidad);
        Assert.Equal(2, politicas.Para(EtapaTipo.Consejo).MaxEvaluadores);
        Assert.True(politicas.Para(EtapaTipo.Consejo).PermiteReintento);
    }

    [Theory]
    [InlineData("Flujo:Etapas:EtapaInventada:MaxEvaluadores", "2", "EtapaInventada")]
    [InlineData("Flujo:Etapas:Consejo:MaxEvaluadores", "0", "MaxEvaluadores")]
    [InlineData("Flujo:Etapas:Consejo:MaxEvaluadores", "11", "MaxEvaluadores")]
    public void Una_configuracion_invalida_se_rechaza_al_leerla(string clave, string valor, string esperado)
    {
        using var provider = Construir(new(Minima) { [clave] = valor });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FlujoOptions>>().Value);

        Assert.Contains(esperado, ex.Message, StringComparison.Ordinal);
    }
}
