using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Infrastructure;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.IntegrationTests.Persistence;

public class DatabaseConfigurationTests
{
    private const string ValidConnection =
        "Server=(none);Database=pruebas;User ID=sgt_app_test;Password=marcador;Encrypt=True;Connect Timeout=15";

    private sealed class UsuarioAnonimo : SGT.Tesis.Application.Common.Abstractions.ICurrentUser
    {
        public Guid? UserId => null;

        public string? Rol => null;
    }

    private static ServiceProvider Build(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<SGT.Tesis.Application.Common.Abstractions.ICurrentUser>(new UsuarioAnonimo());
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    [Trait("AC", "16.3")]
    public void Usa_estrategia_con_reintentos_y_timeout_configurados()
    {
        using var provider = Build(new()
        {
            ["ConnectionStrings:DefaultConnection"] = ValidConnection,
            ["Database:CommandTimeoutSeconds"] = "45",
            ["Database:MaxRetryCount"] = "7",
        });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(context.Database.CreateExecutionStrategy().RetriesOnFailure);
        Assert.Equal(45, context.Database.GetCommandTimeout());
    }

    [Fact]
    [Trait("AC", "16.3")]
    public void Sin_reintentos_configurados_la_estrategia_no_reintenta()
    {
        using var provider = Build(new()
        {
            ["ConnectionStrings:DefaultConnection"] = ValidConnection,
            ["Database:MaxRetryCount"] = "0",
        });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(context.Database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    [Trait("AC", "16.4")]
    public void Sin_cadena_de_conexion_la_configuracion_falla_con_mensaje_claro()
    {
        using var provider = Build([]);

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        Assert.Contains("ConnectionStrings", ex.Message);
    }

    [Theory]
    [Trait("AC", "16.2")]
    [InlineData("sa")]
    [InlineData("SA")]
    [InlineData("dbo")]
    [InlineData("admin")]
    public void Rechaza_cuentas_administrativas(string login)
    {
        using var provider = Build(new()
        {
            ["ConnectionStrings:DefaultConnection"] = $"Server=(none);Database=x;User ID={login};Password=marcador",
        });

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        Assert.Contains("administrativa", ex.Message);
    }

    [Theory]
    [Trait("AC", "16.3")]
    [InlineData("Database:CommandTimeoutSeconds", "1")]
    [InlineData("Database:CommandTimeoutSeconds", "999")]
    [InlineData("Database:MaxRetryCount", "-1")]
    [InlineData("Database:MaxRetryCount", "50")]
    [InlineData("Database:MaxRetryDelaySeconds", "0")]
    public void Rechaza_valores_fuera_de_rango(string clave, string valor)
    {
        using var provider = Build(new()
        {
            ["ConnectionStrings:DefaultConnection"] = ValidConnection,
            [clave] = valor,
        });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
    }

    [Fact]
    [Trait("AC", "16.4")]
    public void Los_archivos_de_configuracion_versionados_no_contienen_credenciales()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "SGT.Tesis.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(dir);
        var api = Path.Combine(dir, "src", "SGT.Tesis.API");
        var archivos = Directory.GetFiles(api, "appsettings*.json");
        Assert.NotEmpty(archivos);

        foreach (var archivo in archivos)
        {
            var texto = File.ReadAllText(archivo);

            Assert.DoesNotContain("password=", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pwd=", texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("AC", "16.4")]
    public void Staging_y_Produccion_no_versionan_cadena_de_conexion()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "SGT.Tesis.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(dir);

        foreach (var ambiente in new[] { "Staging", "Production" })
        {
            var texto = File.ReadAllText(Path.Combine(dir, "src", "SGT.Tesis.API", $"appsettings.{ambiente}.json"));

            Assert.DoesNotContain("ConnectionStrings", texto);
        }
    }
}
