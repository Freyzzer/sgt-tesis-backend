using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Flujo;

/// <summary>El estudiante ve su proceso actualizado y se le avisa de cada evaluación (ADR-005).</summary>
public class VisibilidadYAvisosTests
{
    private const string Tesis = "/api/v1/tesis";
    private const string Comentario = "Comentario privado del evaluador.";

    private sealed record Escenario(
        ApiFactory Factory,
        Guid TesisId,
        string CorreoEstudiante,
        HttpClient Estudiante,
        HttpClient Admin,
        Guid EvaluadorId,
        string CorreoEvaluador,
        HttpClient Evaluador);

    private static string Etapa(Guid tesisId) => $"{Tesis}/{tesisId}/etapas/1";

    private static async Task<Escenario> NuevoAsync(ApiFactory factory, bool enviar = true)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();
        var admin = await factory.ClienteAdministradorAsync();
        var (evaluadorId, correoEv) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var evaluador = await factory.ClienteComoAsync(correoEv);

        if (enviar)
        {
            Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Etapa(tesisId)}/enviar", null)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Etapa(tesisId)}/evaluadores", new { evaluadorId })).StatusCode);

        return new Escenario(factory, tesisId, correo, estudiante, admin, evaluadorId, correoEv, evaluador);
    }

    private static Task<HttpResponseMessage> Emitir(Escenario e, string concepto) =>
        e.Evaluador.PutAsJsonAsync($"{Etapa(e.TesisId)}/veredicto", new { concepto, comentarios = Comentario });

    private static async Task<System.Text.Json.JsonElement> Retroalimentacion(Escenario e) =>
        (await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}/retroalimentacion", TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("etapas")[0];

    private static async Task ProcesarCorreosAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
    }

    // ---------- Visibilidad ----------

    [Fact]
    public async Task El_estudiante_ve_cada_veredicto_apenas_se_emite_sin_esperar_la_decision()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);

        Assert.Equal(0, (await Retroalimentacion(e)).GetProperty("veredictos").GetArrayLength());

        await Emitir(e, "ConObservaciones");
        var etapa = await Retroalimentacion(e);
        var veredicto = Assert.Single(etapa.GetProperty("veredictos").EnumerateArray());

        Assert.Equal(0, etapa.GetProperty("decisiones").GetArrayLength());
        Assert.Equal("Evaluador 1", veredicto.GetProperty("evaluador").GetString());
        Assert.Equal("ConObservaciones", veredicto.GetProperty("concepto").GetString());
        Assert.Equal(Comentario, veredicto.GetProperty("comentarios").GetString());
        Assert.Equal(1, veredicto.GetProperty("ronda").GetInt32());

        // Si el evaluador cambia de opinión, el estudiante ve solo la versión vigente.
        await Emitir(e, "Favorable");
        var actualizado = Assert.Single((await Retroalimentacion(e)).GetProperty("veredictos").EnumerateArray());
        Assert.Equal("Favorable", actualizado.GetProperty("concepto").GetString());
    }

    [Fact]
    public async Task La_retroalimentacion_conserva_los_veredictos_de_cada_ronda()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await Emitir(e, "ConObservaciones");
        await e.Admin.PostAsJsonAsync($"{Etapa(e.TesisId)}/decisiones", new { resultado = "Observada", justificacion = "Corregir." }, cancellationToken: TestContext.Current.CancellationToken);
        await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken);
        await Emitir(e, "Favorable");

        var etapa = await Retroalimentacion(e);
        var veredictos = etapa.GetProperty("veredictos").EnumerateArray().ToList();

        Assert.Equal(2, etapa.GetProperty("ronda").GetInt32());
        Assert.Equal([1, 2], veredictos.Select(v => v.GetProperty("ronda").GetInt32()));
        Assert.Equal(["ConObservaciones", "Favorable"], veredictos.Select(v => v.GetProperty("concepto").GetString()));
        Assert.All(veredictos, v => Assert.Equal("Evaluador 1", v.GetProperty("evaluador").GetString()));
        Assert.Equal(1, etapa.GetProperty("decisiones")[0].GetProperty("ronda").GetInt32());
    }

    [Fact]
    public async Task Por_defecto_el_evaluador_es_anonimo_y_con_la_opcion_activada_se_muestra_su_nombre()
    {
        using var anonima = new ApiFactory();
        var a = await NuevoAsync(anonima);
        await Emitir(a, "Favorable");
        var textoAnonimo = (await Retroalimentacion(a)).GetRawText();

        using var conNombre = new ApiFactory();
        conNombre.Configuracion["Flujo:MostrarNombreEvaluador"] = "true";
        var b = await NuevoAsync(conNombre);
        await Emitir(b, "Favorable");
        var textoNombre = (await Retroalimentacion(b)).GetRawText();

        Assert.DoesNotContain("Ana Pérez", textoAnonimo, StringComparison.Ordinal);
        Assert.DoesNotContain(a.CorreoEvaluador, textoAnonimo, StringComparison.Ordinal);
        Assert.Contains("Ana Pérez", textoNombre, StringComparison.Ordinal);
        Assert.DoesNotContain(b.CorreoEvaluador, textoNombre, StringComparison.Ordinal);
    }

    // ---------- Avisos ----------

    [Fact]
    public async Task Cada_veredicto_emitido_o_actualizado_avisa_al_estudiante_sin_revelar_su_contenido()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await ProcesarCorreosAsync(factory);
        var antes = factory.Correos.Enviados.Count;

        await Emitir(e, "ConObservaciones");
        await Emitir(e, "Favorable");
        await ProcesarCorreosAsync(factory);

        var nuevos = factory.Correos.Enviados.Skip(antes).ToList();

        Assert.Equal(2, nuevos.Count);
        Assert.All(nuevos, c =>
        {
            Assert.Equal(e.CorreoEstudiante, c.Destinatario);
            Assert.Contains("nueva evaluación", c.Asunto, StringComparison.Ordinal);
            Assert.DoesNotContain(Comentario, c.Cuerpo, StringComparison.Ordinal);
            Assert.DoesNotContain("Favorable", c.Cuerpo, StringComparison.Ordinal);
            Assert.DoesNotContain("Observaciones", c.Cuerpo, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Al_reenviar_una_etapa_se_avisa_a_los_evaluadores_asignados_con_la_ronda_nueva()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await e.Admin.PostAsJsonAsync($"{Etapa(e.TesisId)}/decisiones", new { resultado = "Observada", justificacion = "Corregir." }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarCorreosAsync(factory);
        var antes = factory.Correos.Enviados.Count;

        await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken);
        await ProcesarCorreosAsync(factory);

        var paraElEvaluador = factory.Correos.Enviados.Skip(antes).Where(c => c.Destinatario == e.CorreoEvaluador).ToList();
        var correo = Assert.Single(paraElEvaluador);

        Assert.Contains("lista para evaluar", correo.Asunto, StringComparison.Ordinal);
        Assert.Contains("ronda 2", correo.Cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_primer_envio_avisa_a_los_evaluadores_ya_asignados_y_no_a_quien_fue_retirado()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory, enviar: false);
        var (retiradoId, correoRetirado) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        await e.Admin.PostAsJsonAsync($"{Etapa(e.TesisId)}/evaluadores", new { evaluadorId = retiradoId }, cancellationToken: TestContext.Current.CancellationToken);
        await e.Admin.DeleteAsync($"{Etapa(e.TesisId)}/evaluadores/{retiradoId}", TestContext.Current.CancellationToken);
        await ProcesarCorreosAsync(factory);
        var antes = factory.Correos.Enviados.Count;

        await e.Estudiante.PostAsync($"{Etapa(e.TesisId)}/enviar", null, TestContext.Current.CancellationToken);
        await ProcesarCorreosAsync(factory);

        var destinatarios = factory.Correos.Enviados.Skip(antes).Select(c => c.Destinatario).ToList();

        Assert.Contains(e.CorreoEvaluador, destinatarios);
        Assert.DoesNotContain(correoRetirado, destinatarios);
    }
}
