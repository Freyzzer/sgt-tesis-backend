using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Infrastructure;
using SGT.Tesis.Infrastructure.Documentos;

namespace SGT.Tesis.IntegrationTests.Documentos;

public class DocumentosConfigurationTests
{
    private sealed class UsuarioAnonimo : ICurrentUser
    {
        public Guid? UserId => null;

        public string? Rol => null;
    }

    private static ServiceProvider Build(Dictionary<string, string?> valores)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new UsuarioAnonimo());
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    [Trait("NFR", "03")]
    public void Sin_ruta_de_almacenamiento_la_configuracion_es_invalida()
    {
        using var provider = Build([]);

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<StorageOptions>>().Value);

        Assert.Contains("Storage:RootPath", ex.Message);
    }

    [Theory]
    [Trait("AC", "7.2")]
    [InlineData("10")]
    [InlineData("999999999999")]
    public void El_tamano_maximo_debe_estar_entre_1_KB_y_100_MB(string maximo)
    {
        using var provider = Build(new() { ["Documentos:MaxBytes"] = maximo });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DocumentosOptions>>().Value);
    }

    [Fact]
    [Trait("AC", "6.2")]
    public void Los_requisitos_deben_nombrar_etapas_validas()
    {
        using var provider = Build(new() { ["Documentos:Requisitos:EtapaInventada:0"] = "Algo" });

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DocumentosOptions>>().Value);

        Assert.Contains("EtapaInventada", ex.Message);
    }

    [Fact]
    [Trait("AC", "6.2")]
    public void Los_requisitos_validos_se_leen_por_etapa()
    {
        using var provider = Build(new() { ["Documentos:Requisitos:Propuesta:0"] = "Anteproyecto" });

        var politica = provider.GetRequiredService<SGT.Tesis.Application.Documentos.IDocumentosPolicy>();

        Assert.Equal(["Anteproyecto"], politica.CategoriasRequeridas(SGT.Tesis.Domain.GestionTesis.EtapaTipo.Propuesta));
        Assert.Empty(politica.CategoriasRequeridas(SGT.Tesis.Domain.GestionTesis.EtapaTipo.Consejo));
    }
}
