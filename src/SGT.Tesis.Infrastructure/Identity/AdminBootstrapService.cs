using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Infrastructure.Identity;

/// <summary>Parámetros opcionales para crear el primer Administrador.</summary>
public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string? AdminCorreo { get; set; }

    public string? AdminPassword { get; set; }

    public string AdminNombres { get; set; } = "Administrador";

    public string AdminApellidos { get; set; } = "Inicial";
}

/// <summary>
/// Crea el primer Administrador si está configurado y aún no existe ninguno. Sin esta cuenta nadie
/// podría crear usuarios. Las variables <c>Bootstrap__*</c> deben retirarse tras el primer arranque.
/// </summary>
internal sealed class AdminBootstrapService(
    IServiceScopeFactory scopes,
    IOptions<BootstrapOptions> options,
    ILogger<AdminBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.AdminCorreo) || string.IsNullOrWhiteSpace(config.AdminPassword))
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();

        if (await usuarios.ExisteAdministradorAsync(cancellationToken))
        {
            logger.LogInformation("Ya existe un Administrador; se ignora la configuración Bootstrap. Retire las variables Bootstrap__*.");
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var usuario = Usuario.Crear(config.AdminNombres, config.AdminApellidos, config.AdminCorreo, hasher.Hash(config.AdminPassword), Rol.Administrador);
        usuarios.Agregar(usuario);
        scope.ServiceProvider.GetRequiredService<IAuditTrail>()
            .Registrar(AccionesAuditoria.AdminInicialCreado, "Usuario", usuario.Id, actorId: usuario.Id);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        logger.LogWarning("Se creó el Administrador inicial {UsuarioId}. Retire las variables Bootstrap__* del entorno.", usuario.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
