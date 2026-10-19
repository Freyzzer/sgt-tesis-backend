using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Notificaciones;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.Infrastructure.Notificaciones;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>Smtp</c> envía correos reales; <c>Log</c> solo los registra (únicamente Development y pruebas).</summary>
    public string Modo { get; set; } = "Log";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>STARTTLS en el puerto indicado (recomendado). Si es falso se usa la negociación automática de MailKit.</summary>
    public bool UseStartTls { get; set; } = true;

    public string User { get; set; } = string.Empty;

    /// <summary>Secreto: variable de entorno <c>Email__Password</c> o gestor de secretos; nunca se versiona.</summary>
    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "SGT-Tesis";

    public int MaxIntentos { get; set; } = 5;

    public int EsperaBaseSegundos { get; set; } = 60;

    public int IntervaloSegundos { get; set; } = 30;

    public int LoteMaximo { get; set; } = 20;

    /// <summary>Permite desactivar el proceso en segundo plano (por ejemplo, en pruebas).</summary>
    public bool WorkerHabilitado { get; set; } = true;
}

internal sealed class EmailOptionsValidator(IHostEnvironment entorno) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var errors = new List<string>();
        var smtp = string.Equals(options.Modo, "Smtp", StringComparison.OrdinalIgnoreCase);
        var log = string.Equals(options.Modo, "Log", StringComparison.OrdinalIgnoreCase);

        if (!smtp && !log)
        {
            errors.Add("Email:Modo debe ser 'Smtp' o 'Log'.");
        }

        if (log && !entorno.IsDevelopment() && !entorno.IsEnvironment("Testing"))
        {
            errors.Add("Email:Modo=Log solo se permite en Development. Configure Email:Modo=Smtp con su servidor de correo para que los avisos lleguen a las personas.");
        }

        if (smtp)
        {
            if (string.IsNullOrWhiteSpace(options.Host))
            {
                errors.Add("Email:Host es obligatorio con Email:Modo=Smtp.");
            }

            if (string.IsNullOrWhiteSpace(options.FromAddress))
            {
                errors.Add("Email:FromAddress es obligatorio con Email:Modo=Smtp.");
            }

            if (options.Port is < 1 or > 65535)
            {
                errors.Add("Email:Port debe estar entre 1 y 65535.");
            }
        }

        if (options.MaxIntentos is < 1 or > 20)
        {
            errors.Add("Email:MaxIntentos debe estar entre 1 y 20.");
        }

        if (options.EsperaBaseSegundos is < 1 or > 3600)
        {
            errors.Add("Email:EsperaBaseSegundos debe estar entre 1 y 3600.");
        }

        if (options.IntervaloSegundos is < 1 or > 3600)
        {
            errors.Add("Email:IntervaloSegundos debe estar entre 1 y 3600.");
        }

        if (options.LoteMaximo is < 1 or > 500)
        {
            errors.Add("Email:LoteMaximo debe estar entre 1 y 500.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken);
}

/// <summary>Solo registra el envío (sin el cuerpo) para desarrollo local.</summary>
internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        logger.LogInformation("Correo simulado a {Destinatario}: {Asunto}", destinatario, asunto);
        return Task.CompletedTask;
    }
}

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(
            config.Host,
            config.Port,
            config.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(config.User))
        {
            await cliente.AuthenticateAsync(config.User, config.Password, cancellationToken);
        }

        await cliente.SendAsync(mensaje, cancellationToken);
        await cliente.DisconnectAsync(quit: true, cancellationToken);
    }
}

internal sealed class Notificador(AppDbContext db, IClock clock) : INotificador
{
    public void Encolar(string destinatario, string asunto, string cuerpo) =>
        db.Notificaciones.Add(NotificacionPendiente.Crear(destinatario, asunto, cuerpo, clock.UtcNow));
}

/// <summary>Envía las notificaciones vencidas del outbox; los fallos se reintentan con espera exponencial.</summary>
public sealed class OutboxProcessor(
    AppDbContext db,
    IEmailSender sender,
    IClock clock,
    IOptions<EmailOptions> options,
    ILogger<OutboxProcessor> logger)
{
    public async Task<int> ProcesarPendientesAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        var ahora = clock.UtcNow;

        var pendientes = await db.Notificaciones
            .Where(n => n.Estado == EstadoNotificacion.Pendiente && n.ProximoIntentoEn <= ahora)
            .OrderBy(n => n.ProximoIntentoEn)
            .Take(config.LoteMaximo)
            .ToListAsync(cancellationToken);

        var enviadas = 0;

        foreach (var notificacion in pendientes)
        {
            try
            {
                await sender.EnviarAsync(notificacion.Destinatario, notificacion.Asunto, notificacion.Cuerpo, cancellationToken);
                notificacion.MarcarEnviada(clock.UtcNow);
                enviadas++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notificacion.RegistrarFallo(ex.Message, clock.UtcNow, config.MaxIntentos, TimeSpan.FromSeconds(config.EsperaBaseSegundos));
                logger.LogWarning(
                    "Falló el envío de la notificación {NotificacionId} (intento {Intento}/{Max}): {Error}",
                    notificacion.Id,
                    notificacion.Intentos,
                    config.MaxIntentos,
                    ex.Message);
            }
        }

        if (pendientes.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return enviadas;
    }
}

/// <summary>Proceso en segundo plano que vacía el outbox periódicamente (una sola instancia por despliegue).</summary>
internal sealed class OutboxEmailWorker(
    IServiceScopeFactory scopes,
    IOptions<EmailOptions> options,
    ILogger<OutboxEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerHabilitado)
        {
            return;
        }

        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervaloSegundos));

        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcesarPendientesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al procesar el outbox de correos");
            }
        }
        while (await SiguienteAsync(temporizador, stoppingToken));
    }

    private static async Task<bool> SiguienteAsync(PeriodicTimer temporizador, CancellationToken cancellationToken)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
