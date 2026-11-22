using System.Net;
using System.Net.Http.Json;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Consultas;

public class ConsultasEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Tesis = "/api/v1/tesis";

    private readonly ApiFactory _factory;

    public ConsultasEndpointsTests(ApiFactory factory) => _factory = factory;

    private async Task<(Guid TesisId, HttpClient Estudiante)> NuevaTesisAsync(string titulo, bool enviar)
    {
        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await _factory.ClienteComoAsync(correo);
        var id = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo, resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();

        if (enviar)
        {
            Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Tesis}/{id}/etapas/1/enviar", null)).StatusCode);
        }

        return (id, estudiante);
    }

    private async Task<(Guid Id, HttpClient Cliente)> NuevoEvaluadorAsync()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        return (id, await _factory.ClienteComoAsync(correo));
    }

    // ---------- GET /auth/yo ----------

    [Fact]
    public async Task El_perfil_devuelve_los_datos_de_la_persona_autenticada_y_exige_token()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        var cliente = await _factory.ClienteComoAsync(correo);

        var perfil = await (await cliente.GetAsync("/api/v1/auth/yo", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(id, perfil.GetProperty("id").GetGuid());
        Assert.Equal("Evaluador", perfil.GetProperty("rol").GetString());
        Assert.Equal(correo, perfil.GetProperty("correo").GetString());
        Assert.False(perfil.GetProperty("debeCambiarPassword").GetBoolean());

        using var anonimo = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/v1/auth/yo", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- GET /evaluaciones/mias ----------

    [Fact]
    public async Task El_evaluador_ve_solo_sus_asignaciones_activas_y_el_filtro_de_pendientes_oculta_las_ya_calificadas()
    {
        var (a, _) = await NuevaTesisAsync("Mias-A", enviar: true);
        var (b, _) = await NuevaTesisAsync("Mias-B", enviar: true);
        var (c, _) = await NuevaTesisAsync("Mias-C", enviar: true);
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        var (_, otro) = await NuevoEvaluadorAsync();
        var admin = await _factory.ClienteAdministradorAsync();

        foreach (var t in new[] { a, b, c })
        {
            Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"{Tesis}/{t}/etapas/1/evaluadores", new { evaluadorId }, TestContext.Current.CancellationToken)).StatusCode);
        }

        // c se retira: ya no aparece
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Tesis}/{c}/etapas/1/evaluadores/{evaluadorId}", TestContext.Current.CancellationToken)).StatusCode);
        // a ya fue calificada
        Assert.Equal(HttpStatusCode.OK, (await evaluador.PutAsJsonAsync($"{Tesis}/{a}/etapas/1/veredicto", new { concepto = "Favorable", comentarios = "Bien." }, TestContext.Current.CancellationToken)).StatusCode);

        var todas = await (await evaluador.GetAsync("/api/v1/evaluaciones/mias", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var pendientes = await (await evaluador.GetAsync("/api/v1/evaluaciones/mias?soloPendientes=true", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var delOtro = await (await otro.GetAsync("/api/v1/evaluaciones/mias", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, todas.GetProperty("total").GetInt32());
        Assert.Equal(new[] { a, b }.Order(), todas.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("tesisId").GetGuid()).Order());
        Assert.True(todas.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("tesisId").GetGuid() == a).GetProperty("veredictoEmitido").GetBoolean());

        var soloB = Assert.Single(pendientes.GetProperty("items").EnumerateArray());
        Assert.Equal(b, soloB.GetProperty("tesisId").GetGuid());
        Assert.Equal("EnRevision", soloB.GetProperty("estadoEtapa").GetString());
        Assert.Equal(1, soloB.GetProperty("etapa").GetInt32());

        Assert.Equal(0, delOtro.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Solo_un_evaluador_consulta_sus_evaluaciones_y_la_paginacion_se_valida()
    {
        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await _factory.ClienteComoAsync(correo);
        var admin = await _factory.ClienteAdministradorAsync();
        var (_, correoEv) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        var evaluador = await _factory.ClienteComoAsync(correoEv);

        Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync("/api/v1/evaluaciones/mias", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/evaluaciones/mias", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await evaluador.GetAsync("/api/v1/evaluaciones/mias?page=0", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- GET /tesis (Administrador) ----------

    [Fact]
    public async Task El_administrador_lista_y_filtra_las_tesis_y_los_demas_roles_reciben_403()
    {
        var unico = $"Busq{Guid.NewGuid():N}"[..14];
        var (enRevision, estudiante) = await NuevaTesisAsync($"{unico} revision", enviar: true);
        var (borrador, _) = await NuevaTesisAsync($"{unico} borrador", enviar: false);
        var admin = await _factory.ClienteAdministradorAsync();
        var (_, correoEv) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        var evaluador = await _factory.ClienteComoAsync(correoEv);

        var porTexto = await (await admin.GetAsync($"{Tesis}?texto={unico}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var ids = porTexto.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(2, porTexto.GetProperty("total").GetInt32());
        Assert.Contains(enRevision, ids);
        Assert.Contains(borrador, ids);

        var fila = porTexto.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == enRevision);
        Assert.Equal(1, fila.GetProperty("etapaActual").GetInt32());
        Assert.Equal("EnRevision", fila.GetProperty("estadoEtapaActual").GetString());
        Assert.False(string.IsNullOrWhiteSpace(fila.GetProperty("estudiante").GetString()));

        var pagina = await (await admin.GetAsync($"{Tesis}?texto={unico}&page=1&pageSize=1", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(1, pagina.GetProperty("items").GetArrayLength());
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());

        Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync(Tesis, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.GetAsync(Tesis, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Tesis}?etapa=9", TestContext.Current.CancellationToken)).StatusCode);
    }
}
