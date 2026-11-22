using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SGT.Tesis.Infrastructure.Documentos;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.API.Health;

/// <summary>Verifica que la base de datos responda (con tope de 5 s para no colgar al orquestador).</summary>
public sealed class DatabaseHealthCheck(AppDbContext db, ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            return await db.Database.CanConnectAsync(limite.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falló la verificación de la base de datos");
            return HealthCheckResult.Unhealthy("Error al consultar la base de datos.");
        }
    }
}

/// <summary>Verifica que la carpeta de archivos exista y se pueda escribir (volumen montado y con permisos).</summary>
public sealed class StorageHealthCheck(IOptions<StorageOptions> options, ILogger<StorageHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var raiz = Path.GetFullPath(options.Value.RootPath);
            Directory.CreateDirectory(raiz);
            var prueba = Path.Combine(raiz, $".salud-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(prueba, "ok", cancellationToken);
            File.Delete(prueba);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falló la verificación del almacenamiento de archivos");
            return HealthCheckResult.Unhealthy("El almacenamiento de archivos no está disponible.");
        }
    }
}
