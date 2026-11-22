using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Auth;

public class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Password = "Clave#Segura123";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> Sembrar(Rol rol = Rol.Estudiante, bool activo = true)
    {
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var usuario = Usuario.Crear("Ana", "Pérez", correo, hasher.Hash(Password), rol);

        if (!activo)
        {
            usuario.Desactivar();
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return correo;
    }

    private Task<HttpResponseMessage> Login(string correo, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });

    private Task<HttpResponseMessage> Refresh(string token) =>
        _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = token });

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    [Trait("AC", "1.1")]
    public async Task Login_valido_devuelve_access_y_refresh_con_vigencias_esperadas()
    {
        var correo = await Sembrar();
        var antes = _factory.Reloj.UtcNow;

        var response = await Login(correo, Password);
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
        Assert.True(body.GetProperty("accessTokenExpiraEn").GetDateTime() <= antes.AddMinutes(15).AddSeconds(5));
        Assert.True(body.GetProperty("refreshTokenExpiraEn").GetDateTime() <= antes.AddDays(7).AddSeconds(5));
    }

    [Fact]
    [Trait("AC", "1.1")]
    public async Task El_administrador_inicial_configurado_puede_iniciar_sesion()
    {
        var response = await Login(ApiFactory.AdminCorreo, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("AC", "1.4")]
    public async Task El_access_token_solo_contiene_sub_role_exp_y_jti_sin_datos_personales()
    {
        var correo = await Sembrar(Rol.Evaluador);

        var body = await Json(await Login(correo, Password));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());

        Assert.NotEmpty(jwt.Subject);
        Assert.Equal("Evaluador", jwt.GetClaim("role").Value);
        Assert.NotEmpty(jwt.Id);
        Assert.True(jwt.ValidTo > DateTime.MinValue);

        var permitidos = new[] { "sub", "role", "jti", "exp", "nbf", "iat", "iss", "aud" };
        Assert.All(jwt.Claims, claim => Assert.Contains(claim.Type, permitidos));
        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains('@') || c.Value.Contains("Pérez"));
    }

    [Fact]
    [Trait("AC", "1.2")]
    public async Task Credenciales_invalidas_devuelven_401_con_el_mismo_mensaje_exista_o_no_el_correo()
    {
        var correo = await Sembrar();

        var passwordMala = await Login(correo, "otra-clave");
        var correoInexistente = await Login("nadie@institucion.edu", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, passwordMala.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, correoInexistente.StatusCode);
        Assert.Equal(
            (await Json(passwordMala)).GetProperty("detail").GetString(),
            (await Json(correoInexistente)).GetProperty("detail").GetString());
    }

    [Fact]
    [Trait("AC", "1.2")]
    public async Task Usuario_inactivo_no_puede_iniciar_sesion()
    {
        var correo = await Sembrar(activo: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(correo, Password)).StatusCode);
    }

    [Fact]
    [Trait("AC", "1.3")]
    public async Task Cinco_fallos_consecutivos_bloquean_la_cuenta_15_minutos()
    {
        var correo = await Sembrar();

        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(correo, "mala")).StatusCode);
        }

        // Aun con la clave correcta, la cuenta sigue bloqueada.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(correo, Password)).StatusCode);

        _factory.Reloj.Avanzar(TimeSpan.FromMinutes(16));

        Assert.Equal(HttpStatusCode.OK, (await Login(correo, Password)).StatusCode);
    }

    [Fact]
    [Trait("AC", "1.3")]
    public async Task Un_acceso_exitoso_reinicia_el_contador_de_fallos()
    {
        var correo = await Sembrar();

        for (var i = 0; i < Usuario.MaxIntentosFallidos - 1; i++)
        {
            await Login(correo, "mala");
        }

        Assert.Equal(HttpStatusCode.OK, (await Login(correo, Password)).StatusCode);

        for (var i = 0; i < Usuario.MaxIntentosFallidos - 1; i++)
        {
            await Login(correo, "mala");
        }

        Assert.Equal(HttpStatusCode.OK, (await Login(correo, Password)).StatusCode);
    }

    [Fact]
    [Trait("AC", "1.2")]
    public async Task Login_con_datos_invalidos_devuelve_400_con_detalle_por_campo()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { correo = "", password = "" }, cancellationToken: TestContext.Current.CancellationToken);
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var campos = body.GetProperty("errors").EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();
        Assert.Contains("correo", campos);
        Assert.Contains("password", campos);
    }

    [Fact]
    [Trait("AC", "2.1")]
    public async Task Refresh_valido_entrega_un_par_nuevo_y_revoca_el_anterior()
    {
        var correo = await Sembrar();
        var primero = await Json(await Login(correo, Password));
        var refreshInicial = primero.GetProperty("refreshToken").GetString()!;

        var response = await Refresh(refreshInicial);
        var segundo = await Json(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(refreshInicial, segundo.GetProperty("refreshToken").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(refreshInicial)).StatusCode);
    }

    [Fact]
    [Trait("AC", "2.2")]
    public async Task Reutilizar_un_refresh_ya_usado_revoca_toda_la_cadena()
    {
        var correo = await Sembrar();
        var primero = await Json(await Login(correo, Password));
        var refreshInicial = primero.GetProperty("refreshToken").GetString()!;
        var refreshVigente = (await Json(await Refresh(refreshInicial))).GetProperty("refreshToken").GetString()!;

        // Reutilización del token ya rotado: se considera robo.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(refreshInicial)).StatusCode);

        // El token vigente de la misma cadena también queda revocado.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(refreshVigente)).StatusCode);
    }

    [Fact]
    [Trait("AC", "2.1")]
    public async Task Refresh_expirado_es_rechazado()
    {
        var correo = await Sembrar();
        var refresh = (await Json(await Login(correo, Password))).GetProperty("refreshToken").GetString()!;

        _factory.Reloj.Avanzar(TimeSpan.FromDays(8));

        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(refresh)).StatusCode);
    }

    [Fact]
    [Trait("AC", "2.1")]
    public async Task Refresh_desconocido_es_rechazado()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh("token-que-no-existe")).StatusCode);
    }

    [Fact]
    [Trait("AC", "2.3")]
    public async Task Logout_invalida_el_refresh_token()
    {
        var correo = await Sembrar();
        var refresh = (await Json(await Login(correo, Password))).GetProperty("refreshToken").GetString()!;

        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = refresh }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(refresh)).StatusCode);
    }

    [Fact]
    [Trait("AC", "2.3")]
    public async Task Logout_es_idempotente_con_un_token_desconocido()
    {
        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "desconocido" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public async Task La_auditoria_de_entidades_se_completa_al_guardar()
    {
        var correo = await Sembrar();
        await Login(correo, Password);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usuario = db.Usuarios.Single(u => u.Correo == correo);

        Assert.NotEqual(default, usuario.CreatedAt);
        Assert.All(db.RefreshTokens.Where(t => t.UsuarioId == usuario.Id).ToList(), t => Assert.NotEqual(default, t.CreatedAt));
    }

    [Fact]
    [Trait("AC", "2.1")]
    public async Task Los_refresh_tokens_se_guardan_solo_como_hash()
    {
        var correo = await Sembrar();
        var refresh = (await Json(await Login(correo, Password))).GetProperty("refreshToken").GetString()!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.DoesNotContain(db.RefreshTokens.ToList(), t => t.TokenHash == refresh);
    }
}
