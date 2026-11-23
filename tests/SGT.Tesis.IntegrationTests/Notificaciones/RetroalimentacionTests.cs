using System.Net;
using System.Net.Http.Json;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Notificaciones;

public class RetroalimentacionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RetroalimentacionTests(ApiFactory factory) => _factory = factory;

    private async Task<(Guid TesisId, HttpClient Estudiante, HttpClient Admin)> NuevoAsync()
    {
        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await _factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync("/api/v1/tesis", new { titulo = "T", resumen = "R" })).LeerJsonAsync()).GetProperty("id").GetGuid();

        return (tesisId, estudiante, await _factory.ClienteAdministradorAsync());
    }

    [Fact]
    [Trait("AC", "13.1")]
    [Trait("AC", "13.2")]
    public async Task El_estudiante_ve_decision_justificacion_y_veredictos_vigentes_sin_identificar_a_los_evaluadores()
    {
        var (tesisId, estudiante, admin) = await NuevoAsync();
        var (id1, correo1) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        var (id2, correo2) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var ev1 = await _factory.ClienteComoAsync(correo1);
        using var ev2 = await _factory.ClienteComoAsync(correo2);
        var etapa = $"/api/v1/tesis/{tesisId}/etapas/1";

        await estudiante.PostAsync($"{etapa}/enviar", null, TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId = id1 }, cancellationToken: TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId = id2 }, cancellationToken: TestContext.Current.CancellationToken);
        await ev1.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "Desfavorable", comentarios = "Primera lectura del primero." }, cancellationToken: TestContext.Current.CancellationToken);
        await ev1.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "ConObservaciones", comentarios = "Versión final del primero." }, cancellationToken: TestContext.Current.CancellationToken);
        await ev2.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "Favorable", comentarios = "Opinión del segundo." }, cancellationToken: TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"{etapa}/decisiones", new { resultado = "Observada", justificacion = "Ajustar la metodología." }, cancellationToken: TestContext.Current.CancellationToken);

        var respuesta = await estudiante.GetAsync($"/api/v1/tesis/{tesisId}/retroalimentacion", TestContext.Current.CancellationToken);
        var texto = await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var body = System.Text.Json.JsonDocument.Parse(texto).RootElement;
        var etapa1 = body.GetProperty("etapas")[0];
        var veredictos = etapa1.GetProperty("veredictos").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Observada", etapa1.GetProperty("decisiones")[0].GetProperty("resultado").GetString());
        Assert.Equal("Ajustar la metodología.", etapa1.GetProperty("decisiones")[0].GetProperty("justificacion").GetString());
        Assert.Equal(2, veredictos.Count);
        Assert.Equal(["Evaluador 1", "Evaluador 2"], veredictos.Select(v => v.GetProperty("evaluador").GetString()));
        Assert.Equal("Versión final del primero.", veredictos[0].GetProperty("comentarios").GetString());
        Assert.DoesNotContain("Primera lectura", texto);

        // La evaluación es anónima: ni identificadores ni correos de los evaluadores.
        Assert.DoesNotContain(id1.ToString(), texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(id2.ToString(), texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(correo1, texto);
        Assert.DoesNotContain(correo2, texto);
    }

    [Fact]
    [Trait("AC", "13.1")]
    public async Task Los_veredictos_se_ven_apenas_se_emiten_aunque_la_etapa_no_tenga_decision()
    {
        var (tesisId, estudiante, admin) = await NuevoAsync();
        var (evaluadorId, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await _factory.ClienteComoAsync(correo);
        var etapa = $"/api/v1/tesis/{tesisId}/etapas/1";

        await estudiante.PostAsync($"{etapa}/enviar", null, TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId }, cancellationToken: TestContext.Current.CancellationToken);
        await evaluador.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "Favorable", comentarios = "Todavía sin decisión." }, cancellationToken: TestContext.Current.CancellationToken);

        var body = await (await estudiante.GetAsync($"/api/v1/tesis/{tesisId}/retroalimentacion", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var etapa1 = body.GetProperty("etapas")[0];

        Assert.Equal(0, etapa1.GetProperty("decisiones").GetArrayLength());
        Assert.Equal(1, etapa1.GetProperty("veredictos").GetArrayLength());
        Assert.Equal("Todavía sin decisión.", etapa1.GetProperty("veredictos")[0].GetProperty("comentarios").GetString());
    }

    [Fact]
    [Trait("AC", "13.1")]
    public async Task La_retroalimentacion_recorre_todas_las_etapas_abiertas_en_orden()
    {
        var (tesisId, estudiante, admin) = await NuevoAsync();

        await estudiante.PostAsync($"/api/v1/tesis/{tesisId}/etapas/1/enviar", null, TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"/api/v1/tesis/{tesisId}/etapas/1/decisiones", new { resultado = "Aprobada" }, cancellationToken: TestContext.Current.CancellationToken);

        var body = await (await estudiante.GetAsync($"/api/v1/tesis/{tesisId}/retroalimentacion", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var etapas = body.GetProperty("etapas").EnumerateArray().ToList();

        Assert.Equal([1, 2], etapas.Select(e => e.GetProperty("numero").GetInt32()));
        Assert.Equal("Aprobado", etapas[0].GetProperty("estado").GetString());
        Assert.Equal(1, etapas[0].GetProperty("decisiones").GetArrayLength());
        Assert.Equal(0, etapas[1].GetProperty("decisiones").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "13.1")]
    public async Task Solo_el_dueno_y_el_administrador_ven_la_retroalimentacion()
    {
        var (tesisId, estudiante, admin) = await NuevoAsync();
        var (_, correoOtro) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (evaluadorId, correoEvaluador) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var otro = await _factory.ClienteComoAsync(correoOtro);
        using var evaluador = await _factory.ClienteComoAsync(correoEvaluador);
        using var anonimo = _factory.CreateClient();
        await admin.PostAsJsonAsync($"/api/v1/tesis/{tesisId}/etapas/1/evaluadores", new { evaluadorId }, cancellationToken: TestContext.Current.CancellationToken);
        var ruta = $"/api/v1/tesis/{tesisId}/retroalimentacion";

        Assert.Equal(HttpStatusCode.OK, (await estudiante.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await evaluador.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/tesis/{Guid.NewGuid()}/retroalimentacion", TestContext.Current.CancellationToken)).StatusCode);
    }
}
