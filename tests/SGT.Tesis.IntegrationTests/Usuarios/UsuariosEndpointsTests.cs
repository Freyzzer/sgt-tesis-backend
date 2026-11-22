using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Usuarios;

public class UsuariosEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Base = "/api/v1/usuarios";
    private const string PasswordNueva = "Nueva#Clave2026";

    private readonly ApiFactory _factory;

    public UsuariosEndpointsTests(ApiFactory factory) => _factory = factory;

    private static object NuevoUsuario(string? correo = null, string password = PasswordNueva, string rol = "Estudiante") => new
    {
        nombres = "Luis",
        apellidos = "Gómez",
        correo = correo ?? $"{Guid.NewGuid():N}@institucion.edu",
        password,
        rol,
    };

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Sin_token_se_rechaza_con_401()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Base, NuevoUsuario(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [Trait("AC", "3.1")]
    [InlineData(Rol.Estudiante)]
    [InlineData(Rol.Evaluador)]
    public async Task Los_roles_que_no_son_administrador_reciben_403(Rol rol)
    {
        var (_, correo) = await _factory.SembrarUsuarioAsync(rol);
        using var client = await _factory.ClienteComoAsync(correo);
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Base, NuevoUsuario(), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"{Base}/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"{Base}/{id}/rol", new { rol = "Administrador" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task El_administrador_crea_un_usuario_que_puede_iniciar_sesion_con_su_rol()
    {
        using var admin = await _factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        var response = await admin.PostAsJsonAsync(Base, NuevoUsuario(correo, rol: "Evaluador"), cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(correo, body.GetProperty("correo").GetString());
        Assert.Equal("Evaluador", body.GetProperty("rol").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));

        var tokens = await _factory.IniciarSesionAsync(correo, PasswordNueva);
        Assert.False(string.IsNullOrEmpty(tokens.GetProperty("accessToken").GetString()));
    }

    [Fact]
    [Trait("AC", "17.1")]
    public async Task El_usuario_creado_registra_al_administrador_como_autor()
    {
        using var admin = await _factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        await admin.PostAsJsonAsync(Base, NuevoUsuario(correo), cancellationToken: TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var creado = db.Usuarios.Single(u => u.Correo == correo);
        var administrador = db.Usuarios.Single(u => u.Correo == ApiFactory.AdminCorreo);

        Assert.Equal(administrador.Id, creado.CreatedBy);
    }

    [Fact]
    [Trait("AC", "3.2")]
    public async Task Un_correo_duplicado_devuelve_409_sin_distinguir_mayusculas()
    {
        using var admin = await _factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Base, NuevoUsuario(correo), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Base, NuevoUsuario(correo.ToUpperInvariant()), cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [Trait("AC", "3.1")]
    [InlineData("corta1A!")]
    [InlineData("sinmayusculas123!")]
    [InlineData("SINMINUSCULAS123!")]
    [InlineData("SinNumerosAquiii!")]
    [InlineData("SinSimbolos12345Abc")]
    public async Task Una_contrasena_debil_devuelve_400(string password)
    {
        using var admin = await _factory.ClienteAdministradorAsync();

        var response = await admin.PostAsJsonAsync(Base, NuevoUsuario(password: password), cancellationToken: TestContext.Current.CancellationToken);
        var campos = (await response.LeerJsonAsync()).GetProperty("errors").EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("password", campos);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Un_correo_con_formato_invalido_devuelve_400()
    {
        using var admin = await _factory.ClienteAdministradorAsync();

        var response = await admin.PostAsJsonAsync(Base, NuevoUsuario("no-es-un-correo"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task El_listado_pagina_y_filtra()
    {
        using var factory = new ApiFactory();
        for (var i = 0; i < 3; i++)
        {
            await factory.SembrarUsuarioAsync(Rol.Evaluador);
        }

        await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var admin = await factory.ClienteAdministradorAsync();

        var pagina = await (await admin.GetAsync($"{Base}?page=1&pageSize=2", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var evaluadores = await (await admin.GetAsync($"{Base}?rol=Evaluador", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, pagina.GetProperty("items").GetArrayLength());
        Assert.Equal(5, pagina.GetProperty("total").GetInt32());
        Assert.Equal(3, evaluadores.GetProperty("total").GetInt32());
    }

    [Theory]
    [Trait("AC", "3.1")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task El_listado_rechaza_paginacion_invalida(string consulta)
    {
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Base}?{consulta}", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Obtener_un_usuario_inexistente_devuelve_404()
    {
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Base}/{Guid.NewGuid()}", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.3")]
    public async Task Un_usuario_desactivado_no_puede_autenticarse_ni_renovar_su_sesion()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync();
        var sesion = await _factory.IniciarSesionAsync(correo, TestHelpers.PasswordPrueba);
        using var admin = await _factory.ClienteAdministradorAsync();

        var response = await admin.PatchAsJsonAsync($"{Base}/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.LeerJsonAsync()).GetProperty("activo").GetBoolean());

        using var anonimo = _factory.CreateClient();
        var login = await anonimo.PostAsJsonAsync("/api/v1/auth/login", new { correo, password = TestHelpers.PasswordPrueba }, cancellationToken: TestContext.Current.CancellationToken);
        var refresh = await anonimo.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = sesion.GetProperty("refreshToken").GetString() }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    [Trait("AC", "3.3")]
    public async Task Un_usuario_desactivado_pierde_el_acceso_con_su_access_token_vigente()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Administrador);
        using var otroAdmin = await _factory.ClienteComoAsync(correo);
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.OK, (await otroAdmin.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);

        await admin.PatchAsJsonAsync($"{Base}/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await otroAdmin.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.3")]
    public async Task Reactivar_permite_volver_a_iniciar_sesion()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(activo: false);
        using var admin = await _factory.ClienteAdministradorAsync();

        await admin.PatchAsJsonAsync($"{Base}/{id}/estado", new { activo = true }, cancellationToken: TestContext.Current.CancellationToken);

        var tokens = await _factory.IniciarSesionAsync(correo, TestHelpers.PasswordPrueba);
        Assert.False(string.IsNullOrEmpty(tokens.GetProperty("accessToken").GetString()));
    }

    [Fact]
    [Trait("AC", "3.3")]
    public async Task Un_administrador_no_puede_desactivarse_ni_cambiarse_el_rol_a_si_mismo()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Administrador);
        using var admin = await _factory.ClienteComoAsync(correo);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PatchAsJsonAsync($"{Base}/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PatchAsJsonAsync($"{Base}/{id}/rol", new { rol = "Estudiante" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Cambiar_el_rol_invalida_el_access_token_anterior_y_el_nuevo_refleja_el_rol()
    {
        var (id, correo) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await _factory.ClienteComoAsync(correo);
        using var admin = await _factory.ClienteAdministradorAsync();

        var response = await admin.PatchAsJsonAsync($"{Base}/{id}/rol", new { rol = "Administrador" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Administrador", (await response.LeerJsonAsync()).GetProperty("rol").GetString());

        // El token emitido con el rol anterior ya no vale; uno nuevo sí.
        Assert.Equal(HttpStatusCode.Unauthorized, (await evaluador.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);
        using var nuevo = await _factory.ClienteComoAsync(correo);
        Assert.Equal(HttpStatusCode.OK, (await nuevo.GetAsync(Base, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Cambiar_estado_o_rol_de_un_usuario_inexistente_devuelve_404()
    {
        using var admin = await _factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsJsonAsync($"{Base}/{Guid.NewGuid()}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsJsonAsync($"{Base}/{Guid.NewGuid()}/rol", new { rol = "Evaluador" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }
}
