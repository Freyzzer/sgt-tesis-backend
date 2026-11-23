using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

public class DocumentoTests
{
    private static readonly string Hash = new('A', 64);

    private static Documento Nuevo(string categoria = "Propuesta", int version = 1, long tamano = 10, string? hash = null) =>
        Documento.Crear(Guid.NewGuid(), categoria, "a.pdf", $"{Guid.NewGuid():N}.pdf", "application/pdf", tamano, hash ?? Hash, version);

    [Fact]
    [Trait("AC", "7.4")]
    public void La_categoria_se_normaliza_para_agrupar_versiones()
    {
        Assert.Equal("propuesta firmada", Documento.ClaveDeCategoria("  Propuesta FIRMADA "));
        Assert.Equal("propuesta firmada", Nuevo("  Propuesta FIRMADA ").CategoriaClave);
    }

    [Theory]
    [Trait("AC", "7.3")]
    [InlineData(0)]
    [InlineData(-5)]
    public void Un_tamano_no_positivo_lanza_excepcion(long tamano)
    {
        Assert.Throws<DomainException>(() => Nuevo(tamano: tamano));
    }

    [Fact]
    public void Version_menor_a_1_hash_corto_o_categoria_vacia_lanzan_excepcion()
    {
        Assert.Throws<DomainException>(() => Nuevo(version: 0));
        Assert.Throws<DomainException>(() => Nuevo(hash: "ABC"));
        Assert.Throws<DomainException>(() => Nuevo(categoria: " "));
        Assert.Throws<DomainException>(() => Nuevo(categoria: new string('a', Documento.CategoriaMaxLength + 1)));
    }

    [Fact]
    [Trait("AC", "7.3")]
    public void El_hash_se_guarda_en_mayusculas()
    {
        Assert.Equal(new string('A', 64), Nuevo(hash: new string('a', 64)).HashSha256);
    }

    [Fact]
    [Trait("AC", "7.5")]
    public void Solo_las_etapas_pendientes_u_observadas_admiten_documentos()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "T", "R", DateTime.UtcNow);

        Assert.True(tesis.ExigirEtapaEditable(EtapaTipo.Estudiante).AdmiteDocumentos);

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, DateTime.UtcNow);

        var ex = Assert.Throws<TransicionInvalidaException>(() => tesis.ExigirEtapaEditable(EtapaTipo.Estudiante));
        Assert.Contains("revisión", ex.Message);
        Assert.Throws<TransicionInvalidaException>(() => tesis.ExigirEtapaEditable(EtapaTipo.Propuesta));
    }
}
