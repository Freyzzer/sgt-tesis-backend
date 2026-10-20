using System.Net;
using System.Net.Http.Json;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Flujo;

/// <summary>Rondas de evaluación y regla de unanimidad (ADR-005) a través de la API.</summary>
public class RondasYUnanimidadEndpointsTests
{
    private const string Tesis = "/api/v1/tesis";

    private sealed record Escenario(ApiFactory Factory, Guid TesisId, HttpClient Estudiante, HttpClient Admin, List<(Guid Id, HttpClient Cliente)> Evaluadores);

    private static string Etapa(Guid tesisId) => $"{Tesis}/{tesisId}/etapas/1";

    /// <summary>Tesis con la etapa 1 en revisión y el número indicado de evaluadores asignados.</summary>
    private static async Task<Escenario> NuevoAsync(ApiFactory factory, int evaluadores)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Etapa(tesisId)}/enviar", null)).StatusCode);
        var admin = await factory.ClienteAdministradorAsync();
        var lista = new List<(Guid, HttpClient)>();

        for (var i = 0; i < evaluadores; i++)
        {
            var (id, correoEv) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Etapa(tesisId)}/evaluadores", new { evaluadorId = id })).StatusCode);
            lista.Add((id, await factory.ClienteComoAsync(correoEv)));
        }

        return new Escenario(factory, tesisId, estudiante, admin, lista);
    }

    private static Task<HttpResponseMessage> Veredicto(Escenario e, int evaluador, string concepto) =>
        e.Evaluadores[evaluador].Cliente.PutAsJsonAsync($"{Etapa(e.TesisId)}/veredicto", new { concepto, comentarios = "Comentario." });

    private static Task<HttpResponseMessage> Decidir(Escenario e, string resultado, string? justificacion = null) =>
        e.Admin.PostAsJsonAsync($"{Etapa(e.TesisId)}/decisiones", new { resultado, justificacion });

    // ---------- Unanimidad ----------

    [Fact]
    public async Task Aprobar_exige_que_todos_los_evaluadores_sean_favorables_y_el_mensaje_dice_cuantos_faltan()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 2);

        // Nadie ha emitido
        var sinVeredictos = await Decidir(e, "Aprobada");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinVeredictos.StatusCode);
        Assert.Contains("0 de 2", await sinVeredictos.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        // Uno favorable, otro no
        await Veredicto(e, 0, "Favorable");
        await Veredicto(e, 1, "ConObservaciones");
        var dividido = await Decidir(e, "Aprobada");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dividido.StatusCode);
        Assert.Contains("1 de 2", await dividido.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        // El evaluador corrige su veredicto: ahora hay unanimidad
        await Veredicto(e, 1, "Favorable");
        var aprobada = await Decidir(e, "Aprobada");
        var cuerpo = await aprobada.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, aprobada.StatusCode);
        Assert.Equal(2, cuerpo.GetProperty("etapaActual").GetProperty("numero").GetInt32());
    }

    [Fact]
    public async Task Observar_y_rechazar_no_exigen_unanimidad()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 3);
        await Veredicto(e, 0, "Desfavorable");

        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Observada", "Corregir.")).StatusCode);
    }

    [Fact]
    public async Task Retirar_a_un_evaluador_que_no_era_favorable_deja_de_bloquear()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 2);
        await Veredicto(e, 0, "Favorable");
        await Veredicto(e, 1, "Desfavorable");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Decidir(e, "Aprobada")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await e.Admin.DeleteAsync($"{Etapa(e.TesisId)}/evaluadores/{e.Evaluadores[1].Id}", TestContext.Current.CancellationToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Aprobada")).StatusCode);
    }

    [Fact]
    public async Task Una_etapa_sin_evaluadores_la_aprueba_el_administrador_directamente()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 0);

        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Aprobada")).StatusCode);
    }

    [Fact]
    public async Task La_unanimidad_se_puede_desactivar_por_configuracion()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Flujo:Etapas:Estudiante:ExigeUnanimidad"] = "false";
        var e = await NuevoAsync(factory, 2);

        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Aprobada")).StatusCode);
    }

    // ---------- Rondas ----------

    [Fact]
    public async Task Cada_envio_abre_una_ronda_y_los_veredictos_de_la_ronda_anterior_no_cuentan()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 1);

        var tesis1 = await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(1, tesis1.GetProperty("etapaActual").GetProperty("ronda").GetInt32());

        // Ronda 1: el evaluador aprueba, pero el administrador observa por otro motivo y el estudiante corrige.
        Assert.Equal(1, (await (await Veredicto(e, 0, "Favorable")).LeerJsonAsync()).GetProperty("ronda").GetInt32());
        var observada = await (await Decidir(e, "Observada", "Falta el aval.")).LeerJsonAsync();
        Assert.Equal(1, observada.GetProperty("decisiones")[0].GetProperty("ronda").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);

        // Ronda 2: el veredicto Favorable de la ronda 1 ya no cuenta.
        var tesis2 = await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(2, tesis2.GetProperty("etapaActual").GetProperty("ronda").GetInt32());
        var bloqueada = await Decidir(e, "Aprobada");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bloqueada.StatusCode);
        Assert.Contains("0 de 1", await bloqueada.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);

        // El evaluador vuelve a evaluar la versión nueva.
        var nuevo = await (await Veredicto(e, 0, "Favorable")).LeerJsonAsync();
        Assert.Equal(2, nuevo.GetProperty("ronda").GetInt32());
        Assert.Equal(2, nuevo.GetProperty("version").GetInt32());

        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Aprobada")).StatusCode);
    }

    [Fact]
    public async Task El_administrador_ve_el_historial_completo_y_puede_pedir_solo_los_vigentes_de_la_ronda()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 1);
        await Veredicto(e, 0, "ConObservaciones");
        await Decidir(e, "Observada", "Corregir.");
        await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken);
        await Veredicto(e, 0, "Favorable");

        var todos = await (await e.Admin.GetAsync($"{Etapa(e.TesisId)}/veredictos", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var vigentes = await (await e.Admin.GetAsync($"{Etapa(e.TesisId)}/veredictos?soloVigentes=true", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, todos.GetArrayLength());
        Assert.Equal(1, vigentes.GetArrayLength());
        Assert.Equal(2, vigentes[0].GetProperty("ronda").GetInt32());
        Assert.Equal("Favorable", vigentes[0].GetProperty("concepto").GetString());
    }

    [Fact]
    public async Task Un_rechazo_reintentable_tambien_abre_una_ronda_nueva_al_reenviar()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, 1);
        await Veredicto(e, 0, "Favorable");
        Assert.Equal(HttpStatusCode.Created, (await Decidir(e, "Rechazada", "No cumple.")).StatusCode);

        var reenviada = await (await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, reenviada.GetProperty("etapaActual").GetProperty("ronda").GetInt32());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Decidir(e, "Aprobada")).StatusCode);
    }
}
