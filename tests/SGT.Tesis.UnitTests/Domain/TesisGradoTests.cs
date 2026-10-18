using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

public class TesisGradoTests
{
    private static readonly Guid Estudiante = Guid.NewGuid();

    [Fact]
    [Trait("AC", "17.3")]
    [Trait("AC", "4.5")]
    public void Crear_con_StudentId_vacio_lanza_excepcion_de_dominio()
    {
        var ex = Assert.Throws<DomainException>(() => TesisGrado.Crear(Guid.Empty, "Titulo", "Resumen"));

        Assert.Contains("StudentId", ex.Message);
    }

    [Theory]
    [Trait("AC", "17.3")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_sin_titulo_lanza_excepcion_de_dominio(string? titulo)
    {
        Assert.Throws<DomainException>(() => TesisGrado.Crear(Estudiante, titulo, "Resumen"));
    }

    [Fact]
    [Trait("AC", "17.3")]
    [Trait("AC", "4.4")]
    public void Crear_con_titulo_excedido_lanza_excepcion_de_dominio()
    {
        var titulo = new string('a', TesisGrado.TituloMaxLength + 1);

        Assert.Throws<DomainException>(() => TesisGrado.Crear(Estudiante, titulo, "Resumen"));
    }

    [Fact]
    [Trait("AC", "17.3")]
    public void Crear_con_resumen_excedido_lanza_excepcion_de_dominio()
    {
        var resumen = new string('a', TesisGrado.ResumenMaxLength + 1);

        Assert.Throws<DomainException>(() => TesisGrado.Crear(Estudiante, "Titulo", resumen));
    }

    [Fact]
    [Trait("AC", "17.3")]
    public void Crear_en_los_limites_permitidos_es_valido()
    {
        var tesis = TesisGrado.Crear(
            Estudiante,
            new string('a', TesisGrado.TituloMaxLength),
            new string('b', TesisGrado.ResumenMaxLength));

        Assert.Equal(Estudiante, tesis.StudentId);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public void Crear_asigna_Id_guid_no_vacio_y_recorta_espacios()
    {
        var tesis = TesisGrado.Crear(Estudiante, "  Titulo  ", " Resumen ");

        Assert.NotEqual(Guid.Empty, tesis.Id);
        Assert.Equal("Titulo", tesis.Titulo);
        Assert.Equal("Resumen", tesis.Resumen);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public void Dos_tesis_tienen_Id_distintos()
    {
        var a = TesisGrado.Crear(Estudiante, "A", "A");
        var b = TesisGrado.Crear(Estudiante, "B", "B");

        Assert.NotEqual(a.Id, b.Id);
    }
}
