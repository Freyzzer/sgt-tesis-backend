using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Domain.Notificaciones;
using SGT.Tesis.Infrastructure;
using SGT.Tesis.Infrastructure.Identity;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Contrasenas;

public partial class ContrasenasTests
{
    private const string Cambiar = "/api/v1/auth/cambiar-password";
    private const string Olvide = "/api/v1/auth/olvide-password";
    private const string Restablecer = "/api/v1/auth/restablecer-password";
    private const string PasswordNueva = "Otra#Clave2026x";
    private const string UrlPlantilla = "https://app.institucion.test/restablecer?token={token}";

    [GeneratedRegex(@"token=([A-Za-z0-9_%\-]+)")]
    private static partial Regex TokenEnEnlace();

    private static async Task<int> ProcesarOutboxAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
    }

    private static async Task<string> SolicitarTokenAsync(ApiFactory factory, string correo)
    {
        using var anonimo = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted, (await anonimo.PostAsJsonAsync(Olvide, new { correo })).StatusCode);
        await ProcesarOutboxAsync(factory);

        var correoEnviado = factory.Correos.Enviados.Last(c => c.Destinatario == correo && c.Asunto.Contains("restablecimiento"));
        var coincidencia = TokenEnEnlace().Match(correoEnviado.Cuerpo);
        Assert.True(coincidencia.Success, "El correo debe contener el enlace con el token.");

        return Uri.UnescapeDataString(coincidencia.Groups[1].Value);
    }

    private static ApiFactory NuevaFabrica()
    {
        var factory = new ApiFactory();
        factory.Configuracion["Frontend:UrlRestablecerPassword"] = UrlPlantilla;

        return factory;
    }

    private static async Task<HttpResponseMessage> Login(ApiFactory factory, string correo, string password)
    {
        using var cliente = factory.CreateClient();
        return await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });
    }

    // ---------- Cambio de contraseña ----------

    [Fact]
    [Trait("FR", "15")]
    public async Task Cambiar_la_contrasena_cierra_las_sesiones_avisa_por_correo_y_queda_auditado()
    {
        using var factory = NuevaFabrica();
        var (id, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var sesion = await factory.IniciarSesionAsync(correo, TestHelpers.PasswordPrueba);
        using var cliente = await factory.ClienteComoAsync(correo);

        var respuesta = await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = TestHelpers.PasswordPrueba, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(factory, correo, TestHelpers.PasswordPrueba)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(factory, correo, PasswordNueva)).StatusCode);

        using var anonimo = factory.CreateClient();
        var refresh = await anonimo.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = sesion.GetProperty("refreshToken").GetString() }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        await ProcesarOutboxAsync(factory);
        var aviso = Assert.Single(factory.Correos.Enviados, c => c.Destinatario == correo);
        Assert.Contains("fue cambiada", aviso.Asunto);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.RegistrosAuditoria.AnyAsync(r => r.Accion == "Auth.PasswordCambiada" && r.ActorId == id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Con_la_contrasena_actual_incorrecta_responde_400_y_acumula_intentos_hasta_bloquear()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = await factory.ClienteComoAsync(correo);

        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            var intento = await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = "incorrecta", passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, intento.StatusCode);
        }

        // La cuenta quedó bloqueada: ni siquiera la contraseña correcta permite cambiarla ni iniciar sesión.
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = TestHelpers.PasswordPrueba, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(factory, correo, TestHelpers.PasswordPrueba)).StatusCode);
    }

    [Theory]
    [Trait("FR", "15")]
    [InlineData("corta1A!")]
    [InlineData("sinnumeros#Aaaaaaa")]
    public async Task La_contrasena_nueva_debe_cumplir_la_politica(string nueva)
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = await factory.ClienteComoAsync(correo);

        var respuesta = await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = TestHelpers.PasswordPrueba, passwordNueva = nueva }, cancellationToken: TestContext.Current.CancellationToken);
        var campos = (await respuesta.LeerJsonAsync()).GetProperty("errors").EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("passwordnueva", campos);
        Assert.Equal(HttpStatusCode.OK, (await Login(factory, correo, TestHelpers.PasswordPrueba)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task La_contrasena_nueva_no_puede_ser_igual_a_la_actual_y_el_cambio_exige_sesion()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = await factory.ClienteComoAsync(correo);
        using var anonimo = factory.CreateClient();

        var igual = await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = TestHelpers.PasswordPrueba, passwordNueva = TestHelpers.PasswordPrueba }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, igual.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync(Cambiar, new { passwordActual = "x", passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- Cambio obligatorio de la contraseña temporal ----------

    [Fact]
    [Trait("FR", "15")]
    public async Task Un_usuario_creado_por_el_administrador_debe_cambiar_su_contrasena_antes_de_usar_el_sistema()
    {
        using var factory = NuevaFabrica();
        using var admin = await factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";
        const string temporal = "Temporal#Clave2026";
        await admin.PostAsJsonAsync("/api/v1/usuarios", new { nombres = "Luis", apellidos = "Gómez", correo, password = temporal, rol = "Estudiante" }, cancellationToken: TestContext.Current.CancellationToken);

        var tokens = await factory.IniciarSesionAsync(correo, temporal);
        Assert.True(tokens.GetProperty("debeCambiarPassword").GetBoolean());

        using var cliente = await factory.ClienteComoAsync(correo, temporal);
        var bloqueado = await cliente.GetAsync("/api/v1/tesis/mia", TestContext.Current.CancellationToken);
        var cuerpo = await bloqueado.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Forbidden, bloqueado.StatusCode);
        Assert.Equal("cambio_password_obligatorio", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/v1/tesis", new { titulo = "T", resumen = "R" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        // Cambiar la contraseña y cerrar sesión siguen permitidos.
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync(Cambiar, new { passwordActual = temporal, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var nuevo = await factory.IniciarSesionAsync(correo, PasswordNueva);
        Assert.False(nuevo.GetProperty("debeCambiarPassword").GetBoolean());
        using var libre = await factory.ClienteComoAsync(correo, PasswordNueva);
        Assert.Equal(HttpStatusCode.Created, (await libre.PostAsJsonAsync("/api/v1/tesis", new { titulo = "T", resumen = "R" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task El_cierre_de_sesion_esta_permitido_con_contrasena_temporal()
    {
        using var factory = NuevaFabrica();
        using var admin = await factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";
        const string temporal = "Temporal#Clave2026";
        await admin.PostAsJsonAsync("/api/v1/usuarios", new { nombres = "Luis", apellidos = "Gómez", correo, password = temporal, rol = "Estudiante" }, cancellationToken: TestContext.Current.CancellationToken);
        var sesion = await factory.IniciarSesionAsync(correo, temporal);
        using var cliente = await factory.ClienteComoAsync(correo, temporal);

        var logout = await cliente.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = sesion.GetProperty("refreshToken").GetString() }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task El_administrador_inicial_debe_cambiar_su_contrasena_salvo_que_se_configure_lo_contrario()
    {
        using var factory = NuevaFabrica();
        factory.Configuracion["Bootstrap:ExigirCambioPassword"] = "true";

        var sesion = await factory.IniciarSesionAsync(ApiFactory.AdminCorreo, ApiFactory.AdminPassword);
        using var admin = await factory.ClienteComoAsync(ApiFactory.AdminCorreo, ApiFactory.AdminPassword);

        Assert.True(sesion.GetProperty("debeCambiarPassword").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/dashboard/kpis", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(Cambiar, new { passwordActual = ApiFactory.AdminPassword, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        using var libre = await factory.ClienteComoAsync(ApiFactory.AdminCorreo, PasswordNueva);
        Assert.Equal(HttpStatusCode.OK, (await libre.GetAsync("/api/v1/dashboard/kpis", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- Recuperación ----------

    [Fact]
    [Trait("FR", "15")]
    public async Task Olvide_responde_202_igual_exista_o_no_la_cuenta_y_solo_envia_correo_a_cuentas_activas()
    {
        using var factory = NuevaFabrica();
        var (_, activo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (_, inactivo) = await factory.SembrarUsuarioAsync(Rol.Estudiante, activo: false);
        using var anonimo = factory.CreateClient();

        var respuestas = new[]
        {
            await anonimo.PostAsJsonAsync(Olvide, new { correo = activo }, cancellationToken: TestContext.Current.CancellationToken),
            await anonimo.PostAsJsonAsync(Olvide, new { correo = inactivo }, cancellationToken: TestContext.Current.CancellationToken),
            await anonimo.PostAsJsonAsync(Olvide, new { correo = "nadie@institucion.edu" }, cancellationToken: TestContext.Current.CancellationToken),
        };
        await ProcesarOutboxAsync(factory);

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Accepted, r.StatusCode));
        Assert.Equal([await respuestas[0].Content.ReadAsStringAsync(TestContext.Current.CancellationToken), await respuestas[1].Content.ReadAsStringAsync(TestContext.Current.CancellationToken)], [string.Empty, string.Empty]);
        var correo = Assert.Single(factory.Correos.Enviados);
        Assert.Equal(activo, correo.Destinatario);
        Assert.Contains("https://app.institucion.test/restablecer?token=", correo.Cuerpo);
        Assert.Contains("30 minutos", correo.Cuerpo);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Olvide_con_un_correo_mal_formado_devuelve_400()
    {
        using var factory = NuevaFabrica();
        using var anonimo = factory.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Olvide, new { correo = "no-es-correo" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Olvide, new { correo = "" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Sin_url_configurada_el_correo_incluye_el_codigo()
    {
        using var factory = new ApiFactory();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var anonimo = factory.CreateClient();

        await anonimo.PostAsJsonAsync(Olvide, new { correo }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarOutboxAsync(factory);

        var enviado = Assert.Single(factory.Correos.Enviados);
        Assert.Contains("Use este código", enviado.Cuerpo);
        Assert.Matches("[A-Za-z0-9_-]{60,}", enviado.Cuerpo);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Restablecer_con_el_token_cambia_la_contrasena_levanta_bloqueos_y_cierra_sesiones()
    {
        using var factory = NuevaFabrica();
        var (id, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var sesion = await factory.IniciarSesionAsync(correo, TestHelpers.PasswordPrueba);

        // Bloquear la cuenta con intentos fallidos.
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            await Login(factory, correo, "mala");
        }

        var token = await SolicitarTokenAsync(factory, correo);
        using var anonimo = factory.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(factory, correo, PasswordNueva)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(factory, correo, TestHelpers.PasswordPrueba)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonimo.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = sesion.GetProperty("refreshToken").GetString() }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.RegistrosAuditoria.AnyAsync(r => r.Accion == "Auth.PasswordRestablecida" && r.ActorId == id, cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(await db.RegistrosAuditoria.AnyAsync(r => r.Accion == "Auth.PasswordRestablecimientoSolicitado" && r.ActorId == id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task El_token_solo_sirve_una_vez()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var token = await SolicitarTokenAsync(factory, correo);
        using var anonimo = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);

        var reutilizado = await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = "Distinta#Clave2027y" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, reutilizado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(factory, correo, PasswordNueva)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task El_token_vence_a_los_30_minutos()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var token = await SolicitarTokenAsync(factory, correo);
        using var anonimo = factory.CreateClient();

        factory.Reloj.Avanzar(TimeSpan.FromMinutes(31));

        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(factory, correo, TestHelpers.PasswordPrueba)).StatusCode);
    }

    [Theory]
    [Trait("FR", "15")]
    [InlineData("token-inexistente")]
    [InlineData("")]
    public async Task Un_token_invalido_devuelve_400_con_mensaje_generico(string token)
    {
        using var factory = NuevaFabrica();
        using var anonimo = factory.CreateClient();

        var respuesta = await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Restablecer_exige_una_contrasena_que_cumpla_la_politica_y_no_consume_el_token()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var token = await SolicitarTokenAsync(factory, correo);
        using var anonimo = factory.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = "debil" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Solo_hay_un_enlace_valido_a_la_vez_y_se_limita_la_frecuencia_de_solicitudes()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var anonimo = factory.CreateClient();

        var primero = await SolicitarTokenAsync(factory, correo);

        // Una segunda solicitud inmediata no genera otro correo.
        await anonimo.PostAsJsonAsync(Olvide, new { correo }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarOutboxAsync(factory);
        Assert.Single(factory.Correos.Enviados, c => c.Asunto.Contains("restablecimiento"));

        // Pasado el minuto, el nuevo enlace invalida el anterior.
        factory.Reloj.Avanzar(TimeSpan.FromMinutes(2));
        var segundo = await SolicitarTokenAsync(factory, correo);

        Assert.NotEqual(primero, segundo);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Restablecer, new { token = primero, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await anonimo.PostAsJsonAsync(Restablecer, new { token = segundo, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Una_cuenta_desactivada_despues_de_pedir_el_enlace_no_puede_restablecer()
    {
        using var factory = NuevaFabrica();
        var (id, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var token = await SolicitarTokenAsync(factory, correo);
        using var admin = await factory.ClienteAdministradorAsync();
        using var anonimo = factory.CreateClient();

        await admin.PatchAsJsonAsync($"/api/v1/usuarios/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, (await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("FR", "15")]
    public async Task Restablecer_levanta_el_cambio_obligatorio_de_una_contrasena_temporal()
    {
        using var factory = NuevaFabrica();
        using var admin = await factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";
        await admin.PostAsJsonAsync("/api/v1/usuarios", new { nombres = "Luis", apellidos = "Gómez", correo, password = "Temporal#Clave2026", rol = "Estudiante" }, cancellationToken: TestContext.Current.CancellationToken);
        var token = await SolicitarTokenAsync(factory, correo);
        using var anonimo = factory.CreateClient();

        await anonimo.PostAsJsonAsync(Restablecer, new { token, passwordNueva = PasswordNueva }, cancellationToken: TestContext.Current.CancellationToken);

        var sesion = await factory.IniciarSesionAsync(correo, PasswordNueva);
        Assert.False(sesion.GetProperty("debeCambiarPassword").GetBoolean());
    }

    // ---------- Seguridad del token ----------

    [Fact]
    [Trait("NFR", "01")]
    public async Task El_token_se_guarda_solo_como_hash_y_el_correo_sensible_se_depura_tras_enviarse()
    {
        using var factory = NuevaFabrica();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);

        var token = await SolicitarTokenAsync(factory, correo);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.DoesNotContain(await db.PasswordResetTokens.ToListAsync(cancellationToken: TestContext.Current.CancellationToken), t => t.TokenHash == token);

        var notificacion = await db.Notificaciones.SingleAsync(n => n.Destinatario == correo && n.Asunto.Contains("restablecimiento"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(notificacion.Sensible);
        Assert.Equal(EstadoNotificacion.Enviada, notificacion.Estado);
        Assert.Equal(NotificacionPendiente.CuerpoDepurado, notificacion.Cuerpo);
        Assert.DoesNotContain(token, notificacion.Cuerpo);
    }

    [Fact]
    [Trait("NFR", "01")]
    public async Task El_correo_sensible_tambien_se_depura_si_se_agotan_los_intentos()
    {
        using var factory = NuevaFabrica();
        factory.Configuracion["Email:MaxIntentos"] = "1";
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        factory.Correos.FallosPendientes = 10;
        using var anonimo = factory.CreateClient();

        await anonimo.PostAsJsonAsync(Olvide, new { correo }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarOutboxAsync(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificacion = await db.Notificaciones.SingleAsync(n => n.Destinatario == correo, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(EstadoNotificacion.Fallida, notificacion.Estado);
        Assert.Equal(NotificacionPendiente.CuerpoDepurado, notificacion.Cuerpo);
    }

    [Fact]
    [Trait("NFR", "04")]
    public async Task Los_endpoints_de_recuperacion_tienen_limite_de_solicitudes()
    {
        using var factory = NuevaFabrica();
        factory.Configuracion["RateLimit:LoginPermitPerMinute"] = "3";
        using var anonimo = factory.CreateClient();

        var estados = new List<HttpStatusCode>();

        for (var i = 0; i < 5; i++)
        {
            estados.Add((await anonimo.PostAsJsonAsync(Olvide, new { correo = "alguien@institucion.edu" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        }

        Assert.Equal(3, estados.Count(s => s == HttpStatusCode.Accepted));
        Assert.Equal(2, estados.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    // ---------- Configuración ----------

    private sealed class UsuarioAnonimo : SGT.Tesis.Application.Common.Abstractions.ICurrentUser
    {
        public Guid? UserId => null;

        public string? Rol => null;
    }

    [Theory]
    [Trait("NFR", "03")]
    [InlineData("https://app.institucion.edu/restablecer")]
    [InlineData("/restablecer?token={token}")]
    [InlineData("ftp://app.institucion.edu/{token}")]
    public void La_url_de_restablecimiento_debe_ser_http_absoluta_y_contener_el_token(string url)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Frontend:UrlRestablecerPassword"] = url }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<SGT.Tesis.Application.Common.Abstractions.ICurrentUser>(new UsuarioAnonimo());
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FrontendOptions>>().Value);
    }
}
