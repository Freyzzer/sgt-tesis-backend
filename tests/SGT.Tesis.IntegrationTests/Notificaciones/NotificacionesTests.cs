using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Domain.Notificaciones;
using SGT.Tesis.Infrastructure;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Notificaciones;

public class NotificacionesTests
{
    private const string TituloSecreto = "TituloSecreto-Quimica-9821";
    private const string JustificacionSecreta = "JustificacionSecreta-metodologia-4417";
    private const string ComentarioSecreto = "ComentarioSecreto-evaluador-7730";

    private sealed record Escenario(ApiFactory Factory, Guid TesisId, string CorreoEstudiante, HttpClient Estudiante, HttpClient Admin);

    private static async Task<Escenario> NuevoAsync(ApiFactory factory)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var estudiante = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await estudiante.PostAsJsonAsync("/api/v1/tesis", new { titulo = TituloSecreto, resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();

        return new Escenario(factory, tesisId, correo, estudiante, await factory.ClienteAdministradorAsync());
    }

    private static string Etapa(Escenario e, int numero = 1) => $"/api/v1/tesis/{e.TesisId}/etapas/{numero}";

    private static async Task<int> ProcesarAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(CancellationToken.None);
    }

    private static async Task<List<NotificacionPendiente>> PendientesAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notificaciones.AsNoTracking().ToListAsync();
    }

    [Fact]
    [Trait("AC", "15.1")]
    public async Task Enviar_una_etapa_notifica_a_los_administradores_activos_pero_solo_tras_procesar_el_outbox()
    {
        using var factory = new ApiFactory();
        var (_, otroAdmin) = await factory.SembrarUsuarioAsync(Rol.Administrador);
        var (_, adminInactivo) = await factory.SembrarUsuarioAsync(Rol.Administrador, activo: false);
        var e = await NuevoAsync(factory);

        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);

        Assert.Empty(factory.Correos.Enviados);
        Assert.Equal(2, await ProcesarAsync(factory));

        var destinatarios = factory.Correos.Enviados.Select(c => c.Destinatario).ToList();
        Assert.Contains(ApiFactory.AdminCorreo, destinatarios);
        Assert.Contains(otroAdmin, destinatarios);
        Assert.DoesNotContain(adminInactivo, destinatarios);
        Assert.All(factory.Correos.Enviados, c => Assert.Contains("Estudiante", c.Cuerpo));
    }

    [Fact]
    [Trait("AC", "9.3")]
    [Trait("AC", "15.1")]
    public async Task Asignar_un_evaluador_le_envia_un_correo()
    {
        using var factory = new ApiFactory();
        var (evaluadorId, correoEvaluador) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var e = await NuevoAsync(factory);

        await e.Admin.PostAsJsonAsync($"{Etapa(e)}/evaluadores", new { evaluadorId }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarAsync(factory);

        var correo = Assert.Single(factory.Correos.Enviados);
        Assert.Equal(correoEvaluador, correo.Destinatario);
        Assert.Contains("asignación", correo.Asunto);
    }

    [Theory]
    [Trait("AC", "15.1")]
    [InlineData("Aprobada", "aprobada")]
    [InlineData("Observada", "observaciones")]
    [InlineData("Rechazada", "rechazada")]
    public async Task Cada_decision_notifica_al_estudiante(string resultado, string palabra)
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);
        await ProcesarAsync(factory);
        var antes = factory.Correos.Enviados.Count;

        await e.Admin.PostAsJsonAsync($"{Etapa(e)}/decisiones", new { resultado, justificacion = JustificacionSecreta }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarAsync(factory);

        var correo = Assert.Single(factory.Correos.Enviados.Skip(antes));
        Assert.Equal(e.CorreoEstudiante, correo.Destinatario);
        Assert.Contains(palabra, correo.Asunto);
    }

    [Fact]
    [Trait("AC", "15.3")]
    public async Task Los_correos_no_incluyen_titulos_nombres_ni_textos_de_justificacion_o_veredictos()
    {
        using var factory = new ApiFactory();
        var (evaluadorId, correoEvaluador) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var e = await NuevoAsync(factory);
        using var evaluador = await factory.ClienteComoAsync(correoEvaluador);

        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);
        await e.Admin.PostAsJsonAsync($"{Etapa(e)}/evaluadores", new { evaluadorId }, cancellationToken: TestContext.Current.CancellationToken);
        await evaluador.PutAsJsonAsync($"{Etapa(e)}/veredicto", new { concepto = "ConObservaciones", comentarios = ComentarioSecreto }, cancellationToken: TestContext.Current.CancellationToken);
        await e.Admin.PostAsJsonAsync($"{Etapa(e)}/decisiones", new { resultado = "Observada", justificacion = JustificacionSecreta }, cancellationToken: TestContext.Current.CancellationToken);
        await ProcesarAsync(factory);

        Assert.NotEmpty(factory.Correos.Enviados);
        Assert.All(factory.Correos.Enviados, c =>
        {
            var texto = $"{c.Asunto}\n{c.Cuerpo}";
            Assert.DoesNotContain(TituloSecreto, texto);
            Assert.DoesNotContain(JustificacionSecreta, texto);
            Assert.DoesNotContain(ComentarioSecreto, texto);
            Assert.DoesNotContain("Ana", texto);
            Assert.DoesNotContain("Pérez", texto);
        });
    }

    [Fact]
    [Trait("AC", "15.2")]
    public async Task Un_fallo_de_correo_no_revierte_la_operacion_y_se_reintenta_con_espera()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);
        await ProcesarAsync(factory);
        factory.Correos.FallosPendientes = 1;

        var respuesta = await e.Admin.PostAsJsonAsync($"{Etapa(e)}/decisiones", new { resultado = "Aprobada" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        // La decisión se guardó aunque el correo falle.
        var tesis = await (await e.Admin.GetAsync($"/api/v1/tesis/{e.TesisId}", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(2, tesis.GetProperty("etapaActual").GetProperty("numero").GetInt32());

        var enviadosAntes = factory.Correos.Enviados.Count;
        Assert.Equal(0, await ProcesarAsync(factory));
        var fallida = (await PendientesAsync(factory)).Single(n => n.Destinatario == e.CorreoEstudiante && n.Intentos > 0);
        Assert.Equal(EstadoNotificacion.Pendiente, fallida.Estado);
        Assert.Equal(1, fallida.Intentos);
        Assert.Contains("no disponible", fallida.UltimoError);
        Assert.True(fallida.ProximoIntentoEn > factory.Reloj.UtcNow);

        // Antes de vencer la espera no se reintenta; después sí.
        Assert.Equal(0, await ProcesarAsync(factory));
        factory.Reloj.Avanzar(TimeSpan.FromMinutes(2));
        Assert.Equal(1, await ProcesarAsync(factory));
        Assert.Equal(enviadosAntes + 1, factory.Correos.Enviados.Count);
        Assert.Equal(EstadoNotificacion.Enviada, (await PendientesAsync(factory)).Single(n => n.Id == fallida.Id).Estado);
    }

    [Fact]
    [Trait("AC", "15.2")]
    public async Task Tras_agotar_los_intentos_la_notificacion_queda_fallida()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Email:MaxIntentos"] = "2";
        var e = await NuevoAsync(factory);
        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);
        factory.Correos.FallosPendientes = 100;

        await ProcesarAsync(factory);
        factory.Reloj.Avanzar(TimeSpan.FromHours(1));
        await ProcesarAsync(factory);
        factory.Reloj.Avanzar(TimeSpan.FromHours(1));
        await ProcesarAsync(factory);

        var todas = await PendientesAsync(factory);
        Assert.All(todas, n => Assert.Equal(EstadoNotificacion.Fallida, n.Estado));
        Assert.All(todas, n => Assert.Equal(2, n.Intentos));
        Assert.Empty(factory.Correos.Enviados);
    }

    [Fact]
    [Trait("AC", "15.2")]
    public async Task Una_operacion_que_falla_no_deja_notificaciones_huerfanas()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);

        // Etapa en Pendiente: decidir es un 409 y no debe encolar ningún correo.
        var respuesta = await e.Admin.PostAsJsonAsync($"{Etapa(e)}/decisiones", new { resultado = "Aprobada" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Empty(await PendientesAsync(factory));
    }

    [Fact]
    [Trait("AC", "15.1")]
    public async Task Las_notificaciones_ya_enviadas_no_se_reenvian()
    {
        using var factory = new ApiFactory();
        var e = await NuevoAsync(factory);
        await e.Estudiante.PostAsync($"{Etapa(e)}/enviar", null, TestContext.Current.CancellationToken);

        var primero = await ProcesarAsync(factory);
        var segundo = await ProcesarAsync(factory);

        Assert.Equal(1, primero);
        Assert.Equal(0, segundo);
    }

    // ---------- Configuración ----------

    private sealed class Entorno(string nombre) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;

        public string ApplicationName { get; set; } = "Pruebas";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class UsuarioAnonimo : ICurrentUser
    {
        public Guid? UserId => null;

        public string? Rol => null;
    }

    private static ServiceProvider Build(string entorno, Dictionary<string, string?> valores)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new Entorno(entorno));
        services.AddSingleton<ICurrentUser>(new UsuarioAnonimo());
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    [Theory]
    [Trait("NFR", "03")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Fuera_de_desarrollo_el_modo_Log_no_esta_permitido(string entorno)
    {
        using var provider = Build(entorno, new() { ["Email:Modo"] = "Log" });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);

        Assert.Contains("Smtp", ex.Message);
    }

    [Fact]
    [Trait("NFR", "03")]
    public void Smtp_exige_servidor_y_remitente()
    {
        using var provider = Build("Production", new() { ["Email:Modo"] = "Smtp" });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);

        Assert.Contains("Email:Host", ex.Message);
        Assert.Contains("Email:FromAddress", ex.Message);
    }

    [Fact]
    [Trait("NFR", "03")]
    public void Smtp_completo_es_valido_y_el_modo_log_se_permite_en_desarrollo()
    {
        using var smtp = Build("Production", new() { ["Email:Modo"] = "Smtp", ["Email:Host"] = "smtp.institucion.edu", ["Email:FromAddress"] = "no-responder@institucion.edu" });
        using var desarrollo = Build("Development", new() { ["Email:Modo"] = "Log" });

        Assert.Equal("Smtp", smtp.GetRequiredService<IOptions<EmailOptions>>().Value.Modo);
        Assert.Equal("Log", desarrollo.GetRequiredService<IOptions<EmailOptions>>().Value.Modo);
    }

    [Theory]
    [Trait("NFR", "03")]
    [InlineData("Email:Modo", "Otro")]
    [InlineData("Email:MaxIntentos", "0")]
    [InlineData("Email:EsperaBaseSegundos", "0")]
    [InlineData("Email:LoteMaximo", "1000")]
    public void Los_valores_invalidos_se_rechazan(string clave, string valor)
    {
        using var provider = Build("Development", new() { [clave] = valor });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
    }
}
