using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Tesis;

public class TesisEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Base = "/api/v1/tesis";

    private readonly ApiFactory _factory;

    public TesisEndpointsTests(ApiFactory factory) => _factory = factory;

    private static object Cuerpo(string titulo = "Modelo predictivo de deserción", string resumen = "Resumen de la propuesta.") =>
        new { titulo, resumen };

    private async Task<(Guid Id, HttpClient Cliente)> NuevoEstudianteAsync()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        return (id, await _factory.ClienteComoAsync(correo));
    }

    [Fact]
    [Trait("AC", "4.1")]
    [Trait("AC", "4.2")]
    public async Task El_estudiante_registra_su_tesis_con_la_etapa_Estudiante_pendiente_y_su_propio_id()
    {
        var (id, estudiante) = await NuevoEstudianteAsync();

        // Aun si el cuerpo trae otro studentId, se ignora: se usa el del token.
        var response = await estudiante.PostAsJsonAsync(Base, new { titulo = "Titulo", resumen = "Resumen", studentId = Guid.NewGuid() }, cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(id, body.GetProperty("studentId").GetGuid());
        Assert.Equal("EnCurso", body.GetProperty("estado").GetString());
        Assert.Equal("Estudiante", body.GetProperty("etapaActual").GetProperty("tipo").GetString());
        Assert.Equal("Pendiente", body.GetProperty("etapaActual").GetProperty("estado").GetString());
        Assert.Equal(1, body.GetProperty("etapas").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "4.3")]
    public async Task Una_segunda_tesis_en_curso_devuelve_409()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();

        Assert.Equal(HttpStatusCode.Created, (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "4.3")]
    public async Task La_base_de_datos_impide_dos_tesis_en_curso_del_mismo_estudiante()
    {
        var (id, _) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ahora = DateTime.UtcNow;
        db.Tesis.Add(TesisGrado.Crear(id, "Uno", "Uno", ahora));
        db.Tesis.Add(TesisGrado.Crear(id, "Dos", "Dos", ahora));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [Trait("AC", "4.4")]
    [InlineData("", "Resumen")]
    [InlineData("Titulo", "")]
    public async Task Datos_vacios_devuelven_400_con_detalle_por_campo(string titulo, string resumen)
    {
        var (_, estudiante) = await NuevoEstudianteAsync();

        var response = await estudiante.PostAsJsonAsync(Base, Cuerpo(titulo, resumen), cancellationToken: TestContext.Current.CancellationToken);
        var campos = (await response.LeerJsonAsync()).GetProperty("errors").EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(titulo.Length == 0 ? "titulo" : "resumen", campos);
    }

    [Fact]
    [Trait("AC", "4.4")]
    public async Task Titulo_o_resumen_excedidos_devuelven_400()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await estudiante.PostAsJsonAsync(Base, Cuerpo(titulo: new string('a', 301)), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await estudiante.PostAsJsonAsync(Base, Cuerpo(resumen: new string('a', 3001)), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "4.1")]
    public async Task Sin_token_el_acceso_es_401_y_otros_roles_no_pueden_registrar_tesis()
    {
        using var anonimo = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        using var admin = await _factory.ClienteAdministradorAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await _factory.ClienteComoAsync(correo);
        Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "5.2")]
    public async Task Mi_tesis_incluye_etapa_actual_estado_y_etapas()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();
        await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken);

        var response = await estudiante.GetAsync($"{Base}/mia", TestContext.Current.CancellationToken);
        var body = await response.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("EnCurso", body.GetProperty("estado").GetString());
        Assert.Equal(1, body.GetProperty("etapaActual").GetProperty("numero").GetInt32());
        Assert.Single(body.GetProperty("etapas").EnumerateArray());
    }

    [Fact]
    [Trait("AC", "5.1")]
    public async Task Sin_tesis_registrada_mi_tesis_devuelve_404()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await estudiante.GetAsync($"{Base}/mia", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "5.1")]
    public async Task Un_estudiante_no_ve_la_tesis_de_otro_pero_el_administrador_si()
    {
        var (_, duenio) = await NuevoEstudianteAsync();
        var id = (await (await duenio.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        var (_, otro) = await NuevoEstudianteAsync();
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.OK, (await duenio.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Base}/{Guid.NewGuid()}", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "5.1")]
    public async Task Un_evaluador_sin_asignacion_no_ve_la_tesis()
    {
        var (_, duenio) = await NuevoEstudianteAsync();
        var id = (await (await duenio.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        var (_, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await _factory.ClienteComoAsync(correo);

        Assert.Equal(HttpStatusCode.NotFound, (await evaluador.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "6.1")]
    public async Task Enviar_la_etapa_actual_la_deja_EnRevision()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();
        var id = (await (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();

        var response = await estudiante.PostAsync($"{Base}/{id}/etapas/1/enviar", null, TestContext.Current.CancellationToken);
        var body = await response.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("EnRevision", body.GetProperty("etapaActual").GetProperty("estado").GetString());

        var persistida = await (await estudiante.GetAsync($"{Base}/mia", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal("EnRevision", persistida.GetProperty("etapaActual").GetProperty("estado").GetString());
    }

    [Fact]
    [Trait("AC", "6.3")]
    public async Task Enviar_una_etapa_que_no_es_la_actual_o_ya_enviada_devuelve_409()
    {
        var (_, estudiante) = await NuevoEstudianteAsync();
        var id = (await (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Conflict, (await estudiante.PostAsync($"{Base}/{id}/etapas/2/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"{Base}/{id}/etapas/1/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await estudiante.PostAsync($"{Base}/{id}/etapas/1/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [Trait("AC", "6.3")]
    [InlineData(0)]
    [InlineData(8)]
    public async Task Un_numero_de_etapa_fuera_de_1_a_7_devuelve_400(int numero)
    {
        var (_, estudiante) = await NuevoEstudianteAsync();
        var id = (await (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await estudiante.PostAsync($"{Base}/{id}/etapas/{numero}/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "6.1")]
    public async Task Solo_el_dueno_puede_enviar_la_etapa()
    {
        var (_, duenio) = await NuevoEstudianteAsync();
        var id = (await (await duenio.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        var (_, otro) = await NuevoEstudianteAsync();
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await otro.PostAsync($"{Base}/{id}/etapas/1/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync($"{Base}/{id}/etapas/1/enviar", null, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public async Task La_tesis_y_sus_etapas_registran_autoria_y_fechas()
    {
        var (estudianteId, estudiante) = await NuevoEstudianteAsync();
        var id = (await (await estudiante.PostAsJsonAsync(Base, Cuerpo(), cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tesis = await db.Tesis.Include(t => t.Etapas).SingleAsync(t => t.Id == id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(estudianteId, tesis.CreatedBy);
        Assert.All(tesis.Etapas, e => Assert.Equal(estudianteId, e.CreatedBy));
        Assert.NotEqual(default, tesis.CreatedAt);
    }
}
