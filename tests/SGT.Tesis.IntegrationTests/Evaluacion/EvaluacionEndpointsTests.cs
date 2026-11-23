using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Evaluacion;

public class EvaluacionEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Tesis = "/api/v1/tesis";

    private readonly ApiFactory _factory;

    public EvaluacionEndpointsTests(ApiFactory factory) => _factory = factory;

    private sealed record Escenario(Guid TesisId, HttpClient Estudiante, HttpClient Admin);

    /// <summary>Estudiante con tesis cuya etapa 1 ya fue enviada a revisión.</summary>
    private async Task<Escenario> EscenarioEnRevisionAsync(bool enviar = true)
    {
        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await _factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();

        if (enviar)
        {
            Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Tesis}/{tesisId}/etapas/1/enviar", null)).StatusCode);
        }

        return new Escenario(tesisId, estudiante, await _factory.ClienteAdministradorAsync());
    }

    private async Task<(Guid Id, HttpClient Cliente)> NuevoEvaluadorAsync()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        return (id, await _factory.ClienteComoAsync(correo));
    }

    private static string Etapa(Guid tesisId, int numero) => $"{Tesis}/{tesisId}/etapas/{numero}";

    private static Task<HttpResponseMessage> Asignar(HttpClient admin, Guid tesisId, Guid evaluadorId, int etapa = 1) =>
        admin.PostAsJsonAsync($"{Etapa(tesisId, etapa)}/evaluadores", new { evaluadorId });

    private static Task<HttpResponseMessage> Decidir(HttpClient admin, Guid tesisId, string resultado, string? justificacion = null, int etapa = 1) =>
        admin.PostAsJsonAsync($"{Etapa(tesisId, etapa)}/decisiones", new { resultado, justificacion });

    private static Task<HttpResponseMessage> Veredicto(HttpClient evaluador, Guid tesisId, string concepto = "Favorable", string comentarios = "Cumple.", int etapa = 1) =>
        evaluador.PutAsJsonAsync($"{Etapa(tesisId, etapa)}/veredicto", new { concepto, comentarios });

    // ---------- US-09: asignar evaluador ----------

    [Fact]
    [Trait("AC", "9.1")]
    public async Task Solo_el_administrador_asigna_evaluadores()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await Asignar(e.Estudiante, e.TesisId, evaluadorId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Asignar(evaluador, e.TesisId, evaluadorId)).StatusCode);

        var respuesta = await Asignar(e.Admin, e.TesisId, evaluadorId);
        var body = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(evaluadorId, body.GetProperty("evaluadorId").GetGuid());
        Assert.True(body.GetProperty("activa").GetBoolean());
    }

    [Fact]
    [Trait("AC", "9.1")]
    public async Task El_asignado_debe_ser_un_evaluador_activo()
    {
        var e = await EscenarioEnRevisionAsync();
        var (estudianteId, _) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (inactivoId, _) = await _factory.SembrarUsuarioAsync(Rol.Evaluador, activo: false);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Asignar(e.Admin, e.TesisId, estudianteId)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Asignar(e.Admin, e.TesisId, inactivoId)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Asignar(e.Admin, e.TesisId, Guid.NewGuid())).StatusCode);
    }

    [Fact]
    [Trait("AC", "9.2")]
    public async Task Una_asignacion_activa_duplicada_devuelve_409_y_se_puede_reasignar_tras_retirarla()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, _) = await NuevoEvaluadorAsync();

        Assert.Equal(HttpStatusCode.Created, (await Asignar(e.Admin, e.TesisId, evaluadorId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Asignar(e.Admin, e.TesisId, evaluadorId)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await e.Admin.DeleteAsync($"{Etapa(e.TesisId, 1)}/evaluadores/{evaluadorId}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await e.Admin.DeleteAsync($"{Etapa(e.TesisId, 1)}/evaluadores/{evaluadorId}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Asignar(e.Admin, e.TesisId, evaluadorId)).StatusCode);

        var historial = await (await e.Admin.GetAsync($"{Etapa(e.TesisId, 1)}/evaluadores", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(2, historial.GetArrayLength());
        Assert.Equal(1, historial.EnumerateArray().Count(a => a.GetProperty("activa").GetBoolean()));
    }

    [Fact]
    [Trait("AC", "9.1")]
    public async Task No_se_asigna_en_una_etapa_cerrada_ni_inexistente()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, _) = await NuevoEvaluadorAsync();

        Assert.Equal(HttpStatusCode.Conflict, (await Asignar(e.Admin, e.TesisId, evaluadorId, etapa: 2)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Asignar(e.Admin, e.TesisId, evaluadorId, etapa: 9)).StatusCode);

        await Decidir(e.Admin, e.TesisId, "Aprobada");
        Assert.Equal(HttpStatusCode.Conflict, (await Asignar(e.Admin, e.TesisId, evaluadorId, etapa: 1)).StatusCode);
    }

    [Fact]
    [Trait("AC", "5.1")]
    public async Task El_evaluador_asignado_ve_la_tesis_y_sus_documentos_y_el_no_asignado_no()
    {
        var e = await EscenarioEnRevisionAsync(enviar: false);
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        var (_, otro) = await NuevoEvaluadorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await evaluador.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).StatusCode);

        await Asignar(e.Admin, e.TesisId, evaluadorId);

        Assert.Equal(HttpStatusCode.OK, (await evaluador.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await evaluador.GetAsync($"{Etapa(e.TesisId, 1)}/documentos", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).StatusCode);

        await e.Admin.DeleteAsync($"{Etapa(e.TesisId, 1)}/evaluadores/{evaluadorId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, (await evaluador.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- US-10: veredicto ----------

    [Fact]
    [Trait("AC", "10.1")]
    public async Task Sin_asignacion_activa_el_veredicto_devuelve_403_y_los_otros_roles_tambien()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await Veredicto(evaluador, e.TesisId)).StatusCode);

        await Asignar(e.Admin, e.TesisId, evaluadorId);
        Assert.Equal(HttpStatusCode.OK, (await Veredicto(evaluador, e.TesisId)).StatusCode);

        await e.Admin.DeleteAsync($"{Etapa(e.TesisId, 1)}/evaluadores/{evaluadorId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await Veredicto(evaluador, e.TesisId)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await Veredicto(e.Estudiante, e.TesisId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Veredicto(e.Admin, e.TesisId)).StatusCode);
    }

    [Fact]
    [Trait("AC", "10.2")]
    public async Task El_veredicto_se_guarda_y_el_administrador_lo_ve()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);

        var respuesta = await Veredicto(evaluador, e.TesisId, "ConObservaciones", "Revisar la metodología.");
        var body = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("ConObservaciones", body.GetProperty("concepto").GetString());
        Assert.Equal(1, body.GetProperty("version").GetInt32());

        var lista = await (await e.Admin.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Single(lista.EnumerateArray());
        Assert.Equal("Revisar la metodología.", lista[0].GetProperty("comentarios").GetString());
        Assert.Equal(evaluadorId, lista[0].GetProperty("evaluadorId").GetGuid());
    }

    [Theory]
    [Trait("AC", "10.2")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Los_comentarios_son_obligatorios_y_acotados(string comentarios)
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);

        Assert.Equal(HttpStatusCode.BadRequest, (await Veredicto(evaluador, e.TesisId, comentarios: comentarios)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Veredicto(evaluador, e.TesisId, comentarios: new string('a', 4001))).StatusCode);
    }

    [Fact]
    [Trait("AC", "10.3")]
    public async Task Emitir_un_veredicto_no_cambia_el_estado_de_la_etapa_ni_de_la_tesis()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);

        await Veredicto(evaluador, e.TesisId, "Desfavorable", "No cumple.");

        var tesis = await (await e.Admin.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnRevision", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal("EnCurso", tesis.GetProperty("estado").GetString());
        Assert.Equal(0, tesis.GetProperty("decisiones").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "10.4")]
    public async Task Actualizar_el_veredicto_crea_una_version_nueva_y_conserva_el_historial()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);

        await Veredicto(evaluador, e.TesisId, "Desfavorable", "Primera lectura.");
        var v2 = await (await Veredicto(evaluador, e.TesisId, "Favorable", "Tras correcciones.")).LeerJsonAsync();

        Assert.Equal(2, v2.GetProperty("version").GetInt32());

        var todos = await (await e.Admin.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var vigentes = await (await e.Admin.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos?soloVigentes=true", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, todos.GetArrayLength());
        Assert.Single(vigentes.EnumerateArray());
        Assert.Equal("Tras correcciones.", vigentes[0].GetProperty("comentarios").GetString());
    }

    [Fact]
    [Trait("AC", "10.4")]
    public async Task Solo_se_emite_veredicto_mientras_la_etapa_esta_en_revision()
    {
        var e = await EscenarioEnRevisionAsync(enviar: false);
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);

        Assert.Equal(HttpStatusCode.Conflict, (await Veredicto(evaluador, e.TesisId)).StatusCode);

        await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 1)}/enviar", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, (await Veredicto(evaluador, e.TesisId)).StatusCode);

        await Decidir(e.Admin, e.TesisId, "Observada", "Corregir.");
        Assert.Equal(HttpStatusCode.Conflict, (await Veredicto(evaluador, e.TesisId)).StatusCode);
    }

    [Fact]
    [Trait("AC", "10.2")]
    public async Task Un_evaluador_solo_ve_sus_propios_veredictos()
    {
        var e = await EscenarioEnRevisionAsync();
        var (id1, ev1) = await NuevoEvaluadorAsync();
        var (id2, ev2) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, id1);
        await Asignar(e.Admin, e.TesisId, id2);
        await Veredicto(ev1, e.TesisId, comentarios: "del primero");
        await Veredicto(ev2, e.TesisId, comentarios: "del segundo");

        var propios = await (await ev1.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var admin = await (await e.Admin.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Single(propios.EnumerateArray());
        Assert.Equal("del primero", propios[0].GetProperty("comentarios").GetString());
        Assert.Equal(2, admin.GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await e.Estudiante.GetAsync($"{Etapa(e.TesisId, 1)}/veredictos", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- US-11: decidir ----------

    [Fact]
    [Trait("AC", "11.1")]
    public async Task Solo_el_administrador_decide()
    {
        var e = await EscenarioEnRevisionAsync();
        var (evaluadorId, evaluador) = await NuevoEvaluadorAsync();
        await Asignar(e.Admin, e.TesisId, evaluadorId);
        using var anonimo = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Decidir(anonimo, e.TesisId, "Aprobada")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Decidir(e.Estudiante, e.TesisId, "Aprobada")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Decidir(evaluador, e.TesisId, "Aprobada")).StatusCode);

        var tesis = await (await e.Admin.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnRevision", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
    }

    [Fact]
    [Trait("AC", "11.2")]
    public async Task Solo_se_decide_desde_EnRevision()
    {
        var e = await EscenarioEnRevisionAsync(enviar: false);

        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(e.Admin, e.TesisId, "Aprobada")).StatusCode);

        await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 1)}/enviar", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, (await Decidir(e.Admin, e.TesisId, "Aprobada")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(e.Admin, e.TesisId, "Aprobada")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(e.Admin, e.TesisId, "Aprobada", etapa: 3)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Decidir(e.Admin, Guid.NewGuid(), "Aprobada")).StatusCode);
    }

    [Theory]
    [Trait("AC", "11.3")]
    [InlineData("Observada")]
    [InlineData("Rechazada")]
    public async Task Observar_o_rechazar_exige_justificacion_y_respeta_el_maximo(string resultado)
    {
        var e = await EscenarioEnRevisionAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Decidir(e.Admin, e.TesisId, resultado)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Decidir(e.Admin, e.TesisId, resultado, "   ")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Decidir(e.Admin, e.TesisId, resultado, new string('a', 2001))).StatusCode);

        var tesis = await (await e.Admin.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnRevision", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
    }

    [Fact]
    [Trait("AC", "11.4")]
    [Trait("AC", "11.5")]
    public async Task Aprobar_abre_la_etapa_siguiente_y_deja_la_decision_en_la_linea_de_tiempo()
    {
        var e = await EscenarioEnRevisionAsync();

        var respuesta = await Decidir(e.Admin, e.TesisId, "Aprobada", "Cumple todo.");
        var tesis = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(2, tesis.GetProperty("etapaActual").GetProperty("numero").GetInt32());
        Assert.Equal("Pendiente", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal(2, tesis.GetProperty("etapas").GetArrayLength());
        Assert.Equal("Aprobado", tesis.GetProperty("etapas")[0].GetProperty("estado").GetString());
        Assert.NotNull(tesis.GetProperty("etapas")[0].GetProperty("fechaCierre").GetString());

        // El estudiante ve la decisión y su justificación (AC-05.2).
        var propia = await (await e.Estudiante.GetAsync($"{Tesis}/mia", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var decision = propia.GetProperty("decisiones")[0];
        Assert.Equal("Aprobada", decision.GetProperty("resultado").GetString());
        Assert.Equal("Cumple todo.", decision.GetProperty("justificacion").GetString());
        Assert.Equal(1, decision.GetProperty("etapa").GetInt32());

        // La etapa siguiente ya admite su propio ciclo.
        Assert.Equal(HttpStatusCode.OK, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 2)}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "11.5")]
    public async Task La_decision_registra_al_administrador_y_la_fecha()
    {
        // Fábrica propia: se adelanta el reloj y no debe afectar a las demás pruebas.
        using var factory = new ApiFactory();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var estudiante = await factory.ClienteComoAsync(correo);
        using var admin = await factory.ClienteAdministradorAsync();
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "T", resumen = "R" }, cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        await estudiante.PostAsync($"{Etapa(tesisId, 1)}/enviar", null, TestContext.Current.CancellationToken);
        var fecha = DateTime.UtcNow.AddMinutes(5);
        factory.Reloj.UtcNow = fecha;

        Assert.Equal(HttpStatusCode.Created, (await Decidir(admin, tesisId, "Observada", "Falta el aval.")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var decision = await db.Decisiones.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var administrador = await db.Usuarios.SingleAsync(u => u.Correo == ApiFactory.AdminCorreo, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(administrador.Id, decision.AdministradorId);
        Assert.Equal(fecha, decision.FechaUtc);
        Assert.Equal(administrador.Id, decision.CreatedBy);
    }

    [Fact]
    [Trait("AC", "11.4")]
    public async Task Observar_devuelve_la_etapa_al_estudiante_que_puede_corregir_y_reenviar()
    {
        var e = await EscenarioEnRevisionAsync();

        await Decidir(e.Admin, e.TesisId, "Observada", "Adjunte la carta de aval.");
        var observada = await (await e.Estudiante.GetAsync($"{Tesis}/mia", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("Observado", observada.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal("Adjunte la carta de aval.", observada.GetProperty("decisiones")[0].GetProperty("justificacion").GetString());

        var form = new MultipartFormDataContent { { new StringContent("Carta de aval"), "categoria" } };
        form.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 aval")), "archivo", "aval.pdf");
        Assert.Equal(HttpStatusCode.Created, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 1)}/documentos", form, TestContext.Current.CancellationToken)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 1)}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Decidir(e.Admin, e.TesisId, "Aprobada")).StatusCode);
    }

    [Fact]
    [Trait("AC", "11.4")]
    public async Task Rechazar_con_politica_definitiva_rechaza_la_tesis_y_el_estudiante_puede_registrar_una_nueva()
    {
        // El comportamiento por defecto (rechazo reintentable) se prueba en PoliticasDeEtapaTests.
        using var factory = new ApiFactory();
        factory.Configuracion["Flujo:Etapas:Estudiante:PermiteReintento"] = "false";
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" }, cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        await estudiante.PostAsync($"{Tesis}/{tesisId}/etapas/1/enviar", null, TestContext.Current.CancellationToken);
        var e = new Escenario(tesisId, estudiante, await factory.ClienteAdministradorAsync());

        var respuesta = await Decidir(e.Admin, e.TesisId, "Rechazada", "No cumple los requisitos.");
        var tesis = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("Rechazada", tesis.GetProperty("estado").GetString());
        Assert.Equal("Rechazado", tesis.GetProperty("etapaActual").GetProperty("estado").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId, 1)}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await e.Estudiante.PostAsJsonAsync(Tesis, new { titulo = "Nueva", resumen = "Otro intento" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "11.4")]
    public async Task Aprobar_las_siete_etapas_finaliza_la_tesis()
    {
        var e = await EscenarioEnRevisionAsync(enviar: false);

        for (var etapa = 1; etapa <= 7; etapa++)
        {
            Assert.Equal(HttpStatusCode.OK, (await e.Estudiante.PostAsync($"{Etapa(e.TesisId, etapa)}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await Decidir(e.Admin, e.TesisId, "Aprobada", etapa: etapa)).StatusCode);
        }

        var tesis = await (await e.Estudiante.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal("Finalizada", tesis.GetProperty("estado").GetString());
        Assert.Equal(7, tesis.GetProperty("etapas").GetArrayLength());
        Assert.Equal(7, tesis.GetProperty("decisiones").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(e.Admin, e.TesisId, "Aprobada", etapa: 7)).StatusCode);
    }

    [Fact]
    [Trait("AC", "11.6")]
    public async Task Modificar_o_borrar_una_decision_es_rechazado_por_la_capa_de_persistencia()
    {
        var e = await EscenarioEnRevisionAsync();
        await Decidir(e.Admin, e.TesisId, "Aprobada");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var decision = await db.Decisiones.FirstAsync(d => db.Tesis.Any(t => t.Id == e.TesisId && t.Etapas.Any(et => et.Id == d.EtapaId)), cancellationToken: TestContext.Current.CancellationToken);
            db.Entry(decision).Property(nameof(Decision.Justificacion)).CurrentValue = "alterada";

            await Assert.ThrowsAsync<DomainException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var decision = await db.Decisiones.FirstAsync(d => db.Tesis.Any(t => t.Id == e.TesisId && t.Etapas.Any(et => et.Id == d.EtapaId)), cancellationToken: TestContext.Current.CancellationToken);
            db.Decisiones.Remove(decision);

            await Assert.ThrowsAsync<DomainException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    [Trait("AC", "11.7")]
    public async Task Dos_decisiones_simultaneas_sobre_la_misma_etapa_producen_409_en_la_segunda()
    {
        var e = await EscenarioEnRevisionAsync();
        var admin = Guid.NewGuid();

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var tesis1 = await scope1.ServiceProvider.GetRequiredService<ITesisRepository>().ObtenerPorIdAsync(e.TesisId, CancellationToken.None);
        var tesis2 = await scope2.ServiceProvider.GetRequiredService<ITesisRepository>().ObtenerPorIdAsync(e.TesisId, CancellationToken.None);

        var ahora = DateTime.UtcNow;
        scope2.ServiceProvider.GetRequiredService<IDecisionRepository>()
            .Agregar(tesis2!.Decidir(Domain(1), ResultadoDecision.Aprobada, admin, null, ahora));
        await scope2.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        scope1.ServiceProvider.GetRequiredService<IDecisionRepository>()
            .Agregar(tesis1!.Decidir(Domain(1), ResultadoDecision.Rechazada, admin, "Rechazo tardío", ahora));

        await Assert.ThrowsAsync<ConflictoException>(
            () => scope1.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        // La primera decisión prevaleció.
        var tesis = await (await e.Admin.GetAsync($"{Tesis}/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnCurso", tesis.GetProperty("estado").GetString());
        Assert.Equal(2, tesis.GetProperty("etapaActual").GetProperty("numero").GetInt32());
        Assert.Equal(1, tesis.GetProperty("decisiones").GetArrayLength());
    }

    private static SGT.Tesis.Domain.GestionTesis.EtapaTipo Domain(int numero) => (SGT.Tesis.Domain.GestionTesis.EtapaTipo)numero;
}
