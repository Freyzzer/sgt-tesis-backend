using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Infrastructure.Notificaciones;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.IntegrationTests.Support;

/// <summary>Doble de <see cref="IEmailSender"/> que guarda los correos y puede simular fallos.</summary>
public sealed class CorreosCapturados : IEmailSender
{
    public sealed record Correo(string Destinatario, string Asunto, string Cuerpo);

    private readonly List<Correo> _enviados = [];

    public IReadOnlyList<Correo> Enviados
    {
        get
        {
            lock (_enviados)
            {
                return [.. _enviados];
            }
        }
    }

    /// <summary>Cantidad de envíos que deben fallar antes de volver a funcionar.</summary>
    public int FallosPendientes { get; set; }

    public Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        if (FallosPendientes > 0)
        {
            FallosPendientes--;
            throw new InvalidOperationException("Servidor de correo no disponible.");
        }

        lock (_enviados)
        {
            _enviados.Add(new Correo(destinatario, asunto, cuerpo));
        }

        return Task.CompletedTask;
    }
}

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

    /// <summary>Carpeta temporal de archivos de esta instancia; se elimina al liberar la fábrica.</summary>
    public string RutaAlmacen { get; } = Path.Combine(Path.GetTempPath(), "sgt-tesis-pruebas", Guid.NewGuid().ToString("N"));

    public FakeClock Reloj { get; } = new();

    /// <summary>Bandeja de correos "enviados" por el outbox durante la prueba.</summary>
    public CorreosCapturados Correos { get; } = new();

    /// <summary>Hace que la base de datos de la API sea inalcanzable (para probar la disponibilidad).</summary>
    public bool BaseDeDatosInaccesible { get; set; }

    public Dictionary<string, string?> Configuracion { get; } = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(none);Database=pruebas;User ID=sgt_app_test;Password=marcador",
        ["Jwt:Issuer"] = "sgt-tesis-pruebas",
        ["Jwt:Audience"] = "sgt-tesis-clientes",
        ["Jwt:SigningKey"] = "clave-de-pruebas-que-supera-los-32-bytes-0123456789",
        ["RateLimit:LoginPermitPerMinute"] = "1000",
        ["Cors:AllowedOrigins:0"] = "http://frontend.test",
        ["Documentos:MaxBytes"] = "1048576",
        ["Dashboard:KpisCacheSegundos"] = "0",
        ["Email:Modo"] = "Log",
        ["Email:WorkerHabilitado"] = "false",
        ["Bootstrap:AdminCorreo"] = AdminCorreo,
        ["Bootstrap:AdminPassword"] = AdminPassword,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        Configuracion["Storage:RootPath"] = RutaAlmacen;

        if (BaseDeDatosInaccesible)
        {
            // Sin base de datos tampoco puede crearse el administrador inicial al arrancar.
            Configuracion["Bootstrap:AdminCorreo"] = null;
            Configuracion["Bootstrap:AdminPassword"] = null;
        }

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Configuracion));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(Correos);
            services.AddSingleton<IEmailSender>(Correos);
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Reloj);

            _connection.Open();
            using (var esquema = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options))
            {
                esquema.Database.EnsureCreated();
            }

            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                if (BaseDeDatosInaccesible)
                {
                    options.UseSqlite("Data Source=/no/existe/sgt.db;Mode=ReadOnly");
                }
                else
                {
                    options.UseSqlite(_connection);
                }

                options.AddInterceptors(
                    provider.GetRequiredService<AuditoriaInterceptor>(),
                    provider.GetRequiredService<InmutabilidadInterceptor>());
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();

        if (Directory.Exists(RutaAlmacen))
        {
            Directory.Delete(RutaAlmacen, recursive: true);
        }
    }
}
