using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SGT.Tesis.API.Extensions;

namespace SGT.Tesis.IntegrationTests.Api;

public class StartupChecksTests
{
    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "Pruebas";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(string? hosts) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AllowedHosts"] = hosts }).Build();

    [Theory]
    [Trait("Issue", "23")]
    [InlineData("Production", "*")]
    [InlineData("Staging", "*")]
    [InlineData("Production", "")]
    [InlineData("Production", "api.institucion.edu;*")]
    public void Fuera_de_desarrollo_rechaza_AllowedHosts_comodin_o_vacio(string ambiente, string hosts)
    {
        Assert.Throws<InvalidOperationException>(
            () => StartupChecks.EnsureAllowedHosts(new FakeEnvironment(ambiente), Config(hosts)));
    }

    [Fact]
    [Trait("Issue", "23")]
    public void Produccion_con_hosts_explicitos_es_valida()
    {
        StartupChecks.EnsureAllowedHosts(new FakeEnvironment("Production"), Config("api.institucion.edu"));
    }

    [Fact]
    [Trait("Issue", "23")]
    public void Development_admite_comodin()
    {
        StartupChecks.EnsureAllowedHosts(new FakeEnvironment("Development"), Config("*"));
    }

    [Fact]
    [Trait("Issue", "23")]
    public void Cors_rechaza_comodin()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "*" })
            .Build();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddApiCors();
        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IOptions<CorsOptions>>().Value);
    }
}
