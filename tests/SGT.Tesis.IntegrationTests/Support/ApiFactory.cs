using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
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
/// API completa en memoria con un reloj controlable. Cada instancia tiene su propia base de datos aislada.
/// Por defecto usa SQLite en memoria; si la variable de entorno <c>SGT_TEST_SQLSERVER</c> contiene la cadena de
/// conexión de un SQL Server (con permiso para crear bases de datos), cada instancia crea y elimina su propia base
/// en ese servidor, de modo que las mismas pruebas verifican el comportamiento real de SQL Server.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminCorreo = "admin@institucion.edu";
    public const string AdminPassword = "Admin#Prueba123";

    public const string VariableSqlServer = "SGT_TEST_SQLSERVER";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string? _servidorSqlServer = Environment.GetEnvironmentVariable(VariableSqlServer);
    private readonly string _nombreBaseSqlServer = $"sgt_test_{Guid.NewGuid():N}";

    /// <summary>Indica si esta ejecución usa SQL Server real en lugar de SQLite.</summary>
    public static bool UsaSqlServer => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableSqlServer));

    private string CadenaSqlServer(string baseDeDatos) =>
        new SqlConnectionStringBuilder(_servidorSqlServer!) { InitialCatalog = baseDeDatos, Pooling = true }.ConnectionString;

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
        ["Bootstrap:ExigirCambioPassword"] = "false",
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

            var opcionesEsquema = new DbContextOptionsBuilder<AppDbContext>();

            if (UsaSqlServer)
            {
                opcionesEsquema.UseSqlServer(CadenaSqlServer(_nombreBaseSqlServer));
            }
            else
            {
                _connection.Open();
                opcionesEsquema.UseSqlite(_connection);
            }

            using (var esquema = new AppDbContext(opcionesEsquema.Options))
            {
                esquema.Database.EnsureCreated();
            }

            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                if (UsaSqlServer)
                {
                    // Un servidor inexistente (puerto 1) con tiempo de espera corto simula la base caída.
                    options.UseSqlServer(BaseDeDatosInaccesible
                        ? "Server=tcp:127.0.0.1,1;Database=no_existe;User Id=x;Password=x;Connect Timeout=2;Encrypt=False"
                        : CadenaSqlServer(_nombreBaseSqlServer));
                }
                else if (BaseDeDatosInaccesible)
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

    private void EliminarBaseSqlServer()
    {
        try
        {
            SqlConnection.ClearAllPools();
            using var conexion = new SqlConnection(CadenaSqlServer("master"));
            conexion.Open();
            using var comando = conexion.CreateCommand();
            comando.CommandText =
                $"IF DB_ID(N'{_nombreBaseSqlServer}') IS NOT NULL BEGIN ALTER DATABASE [{_nombreBaseSqlServer}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_nombreBaseSqlServer}]; END";
            comando.ExecuteNonQuery();
        }
        catch (SqlException)
        {
            // La limpieza es de mejor esfuerzo: una base de pruebas huérfana no debe fallar la prueba.
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();

        if (UsaSqlServer && disposing)
        {
            EliminarBaseSqlServer();
        }

        if (Directory.Exists(RutaAlmacen))
        {
            Directory.Delete(RutaAlmacen, recursive: true);
        }
    }
}
