using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SGT.Tesis.API.Extensions;
using SGT.Tesis.API.Health;
using SGT.Tesis.Infrastructure.Documentos;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Api;

public class HealthYProxyTests
{
    [Fact]
    [Trait("NFR", "10")]
    public async Task Ready_responde_200_cuando_la_base_de_datos_y_el_almacenamiento_estan_disponibles()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var cuerpo = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Healthy", cuerpo.GetProperty("status").GetString());
    }

    [Fact]
    [Trait("NFR", "10")]
    public async Task Con_la_base_de_datos_caida_ready_responde_503_sin_detalles_pero_live_sigue_en_200()
    {
        using var factory = new ApiFactory { BaseDeDatosInaccesible = true };
        using var cliente = factory.CreateClient();

        var ready = await cliente.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var texto = await ready.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var live = await cliente.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Contains("Unhealthy", texto);
        Assert.DoesNotContain("sqlite", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/no/existe", texto);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    [Fact]
    [Trait("NFR", "10")]
    public async Task El_almacenamiento_no_escribible_se_reporta_como_no_saludable()
    {
        var archivo = Path.GetTempFileName();

        try
        {
            // La "carpeta" cuelga de un archivo normal: no se puede crear ni escribir.
            var opciones = Options.Create(new StorageOptions { RootPath = Path.Combine(archivo, "sub") });
            var comprobacion = new StorageHealthCheck(opciones, NullLogger<StorageHealthCheck>.Instance);

            var resultado = await comprobacion.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

            Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        }
        finally
        {
            File.Delete(archivo);
        }
    }

    [Fact]
    [Trait("NFR", "10")]
    public async Task El_healthcheck_del_contenedor_devuelve_1_si_nadie_escucha_y_0_si_responde()
    {
        var anterior = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", "1"); // puerto sin servicio
            Assert.Equal(1, await StartupChecks.EjecutarHealthcheckAsync());
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", anterior);
        }
    }

    // ---------- Proxy inverso ----------

    private static IConfiguration Config(Dictionary<string, string?> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    [Fact]
    [Trait("NFR", "02")]
    public void Sin_habilitar_no_se_confia_en_ninguna_cabecera_reenviada()
    {
        Assert.Null(StartupChecks.LeerProxiesConfiables(Config([])));
        Assert.Null(StartupChecks.LeerProxiesConfiables(Config(new() { ["ForwardedHeaders:Habilitado"] = "false" })));
    }

    [Fact]
    [Trait("NFR", "02")]
    public void Habilitado_exige_redes_confiables_validas()
    {
        Assert.Throws<InvalidOperationException>(
            () => StartupChecks.LeerProxiesConfiables(Config(new() { ["ForwardedHeaders:Habilitado"] = "true" })));
        Assert.Throws<InvalidOperationException>(
            () => StartupChecks.LeerProxiesConfiables(Config(new() { ["ForwardedHeaders:Habilitado"] = "true", ["ForwardedHeaders:RedesConfiables:0"] = "no-es-cidr" })));
    }

    [Fact]
    [Trait("NFR", "02")]
    public void Con_redes_validas_devuelve_las_redes_del_proxy()
    {
        var redes = StartupChecks.LeerProxiesConfiables(Config(new()
        {
            ["ForwardedHeaders:Habilitado"] = "true",
            ["ForwardedHeaders:RedesConfiables:0"] = "10.0.0.0/8",
            ["ForwardedHeaders:RedesConfiables:1"] = "172.16.0.0/12",
        }));

        Assert.Equal(2, redes!.Count);
        Assert.Equal(8, redes[0].PrefixLength);
    }
}
