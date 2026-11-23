using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SGT.Tesis.Infrastructure.Persistence;

/// <summary>
/// Usado solo por las herramientas de EF (migraciones). No abre conexiones: la cadena es un marcador
/// salvo que se defina SGT_DESIGN_CONNECTION (por ejemplo, para generar scripts idempotentes).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SGT_DESIGN_CONNECTION")
            ?? "Server=(none);Database=SGTTesis_Design;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;

        return new AppDbContext(options);
    }
}
