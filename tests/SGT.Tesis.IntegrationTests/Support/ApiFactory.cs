using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.IntegrationTests.Support;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public void Avanzar(TimeSpan tiempo) => UtcNow = UtcNow.Add(tiempo);
}

/// <summary>
/// API completa en memoria con SQLite en lugar de SQL Server y un reloj controlable.
/// Cada instancia tiene su propia base de datos aislada.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminCorreo = "admin@institucion.edu";
    public const string AdminPassword = "Admin#Prueba123";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeClock Reloj { get; } = new();

    public Dictionary<string, string?> Configuracion { get; } = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(none);Database=pruebas;User ID=sgt_app_test;Password=marcador",
        ["Jwt:Issuer"] = "sgt-tesis-pruebas",
        ["Jwt:Audience"] = "sgt-tesis-clientes",
        ["Jwt:SigningKey"] = "clave-de-pruebas-que-supera-los-32-bytes-0123456789",
        ["RateLimit:LoginPermitPerMinute"] = "1000",
        ["Cors:AllowedOrigins:0"] = "http://frontend.test",
        ["Bootstrap:AdminCorreo"] = AdminCorreo,
        ["Bootstrap:AdminPassword"] = AdminPassword,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Configuracion));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Reloj);

            _connection.Open();
            using (var esquema = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options))
            {
                esquema.Database.EnsureCreated();
            }

            services.AddDbContext<AppDbContext>((provider, options) => options
                .UseSqlite(_connection)
                .AddInterceptors(provider.GetRequiredService<AuditoriaInterceptor>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
