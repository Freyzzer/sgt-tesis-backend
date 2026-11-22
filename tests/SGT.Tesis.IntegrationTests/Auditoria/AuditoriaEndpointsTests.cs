using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Domain.Auditoria;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Auditoria;

public class AuditoriaEndpointsTests
{
    private const string Url = "/api/v1/auditoria";

    private static async Task<JsonElement> Consultar(HttpClient admin, string consulta = "")
    {
        var respuesta = await admin.GetAsync($"{Url}{consulta}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        return await respuesta.LeerJsonAsync();
    }

    private static IEnumerable<string> Acciones(JsonElement pagina) =>
        pagina.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("accion").GetString()!);

    [Fact]
    [Trait("AC", "12.1")]
    public async Task Solo_el_administrador_consulta_la_auditoria()
    {
        using var factory = new ApiFactory();
        var (_, correoEstudiante) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (_, correoEvaluador) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var estudiante = await factory.ClienteComoAsync(correoEstudiante);
        using var evaluador = await factory.ClienteComoAsync(correoEvaluador);
        using var anonimo = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Url, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync(Url, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.GetAsync(Url, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "12.2")]
    public async Task Cada_registro_incluye_actor_accion_entidad_ip_y_correlacion()
    {
        using var factory = new ApiFactory();
        var (idEstudiante, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-prueba-123");
        var login = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password = TestHelpers.PasswordPrueba }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("corr-prueba-123", login.Headers.GetValues("X-Correlation-Id").Single());

        using var admin = await factory.ClienteAdministradorAsync();
        var pagina = await Consultar(admin, $"?actorId={idEstudiante}&accion=Auth.Login");
        var registro = pagina.GetProperty("items")[0];

        Assert.Equal("Auth.Login", registro.GetProperty("accion").GetString());
        Assert.Equal("Usuario", registro.GetProperty("entidad").GetString());
        Assert.Equal(idEstudiante, registro.GetProperty("actorId").GetGuid());
        Assert.Equal(idEstudiante, registro.GetProperty("entidadId").GetGuid());
        Assert.Equal("corr-prueba-123", registro.GetProperty("correlacionId").GetString());
        Assert.NotEqual(default, registro.GetProperty("fechaUtc").GetDateTime());
        Assert.True(registro.TryGetProperty("ip", out _));
    }

    [Fact]
    [Trait("AC", "12.2")]
    public async Task Sin_cabecera_valida_se_genera_un_id_de_correlacion_y_se_devuelve()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Correlation-Id", "con espacios y <script>");

        var respuesta = await cliente.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var id = respuesta.Headers.GetValues("X-Correlation-Id").Single();

        Assert.Matches("^[0-9a-f]{32}$", id);
    }

    [Fact]
    [Trait("AC", "1.3")]
    public async Task Los_fallos_de_login_y_el_bloqueo_quedan_en_la_auditoria()
    {
        using var factory = new ApiFactory();
        var (id, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = factory.CreateClient();

        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password = "mala" }, cancellationToken: TestContext.Current.CancellationToken);
        }

        await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password = TestHelpers.PasswordPrueba }, cancellationToken: TestContext.Current.CancellationToken);
        await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo = "nadie@institucion.edu", password = "x" }, cancellationToken: TestContext.Current.CancellationToken);

        using var admin = await factory.ClienteAdministradorAsync();
        var delUsuario = await Consultar(admin, $"?actorId={id}&pageSize=100");
        var acciones = Acciones(delUsuario).ToList();

        Assert.Equal(Usuario.MaxIntentosFallidos, acciones.Count(a => a == "Auth.LoginFallido"));
        Assert.Contains("Auth.CuentaBloqueada", acciones);
        Assert.Contains("Auth.LoginRechazado", acciones);

        var desconocidos = await Consultar(admin, "?accion=Auth.LoginFallido&pageSize=100");
        Assert.Contains(desconocidos.GetProperty("items").EnumerateArray(), r => r.GetProperty("actorId").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    [Trait("AC", "12.1")]
    public async Task El_historial_de_una_tesis_recoge_todo_su_recorrido()
    {
        using var factory = new ApiFactory();
        var (estudianteId, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (evaluadorId, correoEvaluador) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var estudiante = await factory.ClienteComoAsync(correo);
        using var evaluador = await factory.ClienteComoAsync(correoEvaluador);
        using var admin = await factory.ClienteAdministradorAsync();
        // En la realidad cada acción ocurre en un instante distinto; el reloj de prueba es fijo, así que se avanza.
        factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));
        var tesisId = (await (await estudiante.PostAsJsonAsync("/api/v1/tesis", new { titulo = "T", resumen = "R" }, cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        var etapa = $"/api/v1/tesis/{tesisId}/etapas/1";

        factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));
        await estudiante.PostAsync($"{etapa}/enviar", null, TestContext.Current.CancellationToken);
        factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));
        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId }, cancellationToken: TestContext.Current.CancellationToken);
        factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));
        await evaluador.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "Favorable", comentarios = "Cumple." }, cancellationToken: TestContext.Current.CancellationToken);
        factory.Reloj.Avanzar(TimeSpan.FromSeconds(1));
        await admin.PostAsJsonAsync($"{etapa}/decisiones", new { resultado = "Aprobada" }, cancellationToken: TestContext.Current.CancellationToken);

        var historial = await Consultar(admin, $"?tesisId={tesisId}&pageSize=100");
        var acciones = Acciones(historial).Reverse().ToList();

        Assert.Equal(["Tesis.Registrada", "Etapa.Enviada", "Evaluador.Asignado", "Veredicto.Emitido", "Decision.Registrada"], acciones);
        Assert.Equal(estudianteId, historial.GetProperty("items").EnumerateArray().Last().GetProperty("actorId").GetGuid());
    }

    [Fact]
    [Trait("AC", "12.1")]
    public async Task Las_acciones_sobre_usuarios_se_auditan_con_su_autor()
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        var creado = await (await admin.PostAsJsonAsync("/api/v1/usuarios", new { nombres = "Luis", apellidos = "Gómez", correo, password = "Nueva#Clave2026", rol = "Estudiante" }, cancellationToken: TestContext.Current.CancellationToken)).LeerJsonAsync();
        var id = creado.GetProperty("id").GetGuid();
        await admin.PatchAsJsonAsync($"/api/v1/usuarios/{id}/rol", new { rol = "Evaluador" }, cancellationToken: TestContext.Current.CancellationToken);
        await admin.PatchAsJsonAsync($"/api/v1/usuarios/{id}/estado", new { activo = false }, cancellationToken: TestContext.Current.CancellationToken);

        var pagina = await Consultar(admin, "?accion=Usuario.&pageSize=100");
        var items = pagina.GetProperty("items").EnumerateArray().Where(i => i.GetProperty("entidadId").ValueKind != JsonValueKind.Null && i.GetProperty("entidadId").GetGuid() == id).ToList();

        Assert.Equal(
            ["Usuario.Creado", "Usuario.Desactivado", "Usuario.RolCambiado"],
            items.Select(i => i.GetProperty("accion").GetString()!).Order().ToList());
        Assert.Contains("Estudiante -> Evaluador", items.Single(i => i.GetProperty("accion").GetString() == "Usuario.RolCambiado").GetProperty("detalle").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var administrador = await db.Usuarios.SingleAsync(u => u.Correo == ApiFactory.AdminCorreo, cancellationToken: TestContext.Current.CancellationToken);
        Assert.All(items, i => Assert.Equal(administrador.Id, i.GetProperty("actorId").GetGuid()));
    }

    [Fact]
    [Trait("AC", "12.1")]
    public async Task El_administrador_inicial_queda_auditado()
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();

        var pagina = await Consultar(admin, "?accion=Usuario.AdminInicialCreado");

        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
    }

    [Fact]
    [Trait("AC", "12.1")]
    public async Task Los_filtros_por_fecha_y_la_paginacion_funcionan_y_se_validan()
    {
        using var factory = new ApiFactory();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password = TestHelpers.PasswordPrueba }, cancellationToken: TestContext.Current.CancellationToken);
        }

        using var admin = await factory.ClienteAdministradorAsync();
        var primera = await Consultar(admin, "?accion=Auth.Login&pageSize=2&page=1");
        var segunda = await Consultar(admin, "?accion=Auth.Login&pageSize=2&page=2");
        var futuro = await Consultar(admin, $"?desde={Uri.EscapeDataString(DateTime.UtcNow.AddDays(1).ToString("O"))}");

        Assert.Equal(2, primera.GetProperty("items").GetArrayLength());
        Assert.True(primera.GetProperty("total").GetInt32() >= 4);
        Assert.True(segunda.GetProperty("items").GetArrayLength() >= 1);
        Assert.Equal(0, futuro.GetProperty("total").GetInt32());

        foreach (var invalida in new[] { "?page=0", "?pageSize=0", "?pageSize=101", $"?desde={Uri.EscapeDataString("2026-12-01T00:00:00Z")}&hasta={Uri.EscapeDataString("2026-01-01T00:00:00Z")}" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Url}{invalida}", TestContext.Current.CancellationToken)).StatusCode);
        }
    }

    [Fact]
    [Trait("AC", "11.6")]
    public async Task Los_registros_de_auditoria_son_inmutables()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();
        await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo = "nadie@institucion.edu", password = "x" }, cancellationToken: TestContext.Current.CancellationToken);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var registro = await db.RegistrosAuditoria.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
            db.Entry(registro).Property(nameof(RegistroAuditoria.Accion)).CurrentValue = "Alterada";

            await Assert.ThrowsAsync<DomainException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.RegistrosAuditoria.Remove(await db.RegistrosAuditoria.FirstAsync(cancellationToken: TestContext.Current.CancellationToken));

            await Assert.ThrowsAsync<DomainException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    [Trait("AC", "12.2")]
    public void El_registro_recorta_los_campos_largos_y_exige_accion_y_entidad()
    {
        var registro = RegistroAuditoria.Crear(
            "Auth.Login",
            "Usuario",
            Guid.NewGuid(),
            null,
            null,
            new string('d', 5000),
            new string('9', 200),
            new string('c', 200),
            DateTime.UtcNow);

        Assert.Equal(RegistroAuditoria.DetalleMaxLength, registro.Detalle!.Length);
        Assert.Equal(RegistroAuditoria.IpMaxLength, registro.Ip!.Length);
        Assert.Equal(RegistroAuditoria.CorrelacionMaxLength, registro.CorrelacionId!.Length);
        Assert.Throws<DomainException>(() => RegistroAuditoria.Crear(" ", "Usuario", null, null, null, null, null, null, DateTime.UtcNow));
        Assert.Throws<DomainException>(() => RegistroAuditoria.Crear("Accion", "", null, null, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Las_acciones_registradas_no_superan_el_largo_maximo()
    {
        var acciones = typeof(AccionesAuditoria).GetFields().Select(f => (string)f.GetRawConstantValue()!);

        Assert.All(acciones, a => Assert.True(a.Length <= RegistroAuditoria.AccionMaxLength));
    }
}
