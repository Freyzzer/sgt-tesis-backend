using System.IO.Compression;
using System.Text;
using SGT.Tesis.Application.Documentos;

namespace SGT.Tesis.UnitTests.Application;

public class TiposDeArchivoTests
{
    private static MemoryStream Docx(params string[] entradas)
    {
        var memoria = new MemoryStream();

        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entrada in entradas)
            {
                zip.CreateEntry(entrada).Open().Dispose();
            }
        }

        memoria.Position = 0;
        return memoria;
    }

    [Fact]
    [Trait("AC", "7.1")]
    public void Detecta_un_pdf_por_su_firma_y_deja_el_flujo_en_cero()
    {
        using var flujo = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 resto"));

        Assert.Equal(TiposDeArchivo.Pdf, TiposDeArchivo.Detectar(flujo, ".pdf"));
        Assert.Equal(0, flujo.Position);
    }

    [Fact]
    [Trait("AC", "7.1")]
    public void Detecta_un_docx_con_sus_partes_obligatorias()
    {
        using var flujo = Docx("[Content_Types].xml", "word/document.xml");

        Assert.Equal(TiposDeArchivo.Docx, TiposDeArchivo.Detectar(flujo, ".DOCX"));
    }

    [Theory]
    [Trait("AC", "7.1")]
    [InlineData("%PDF-1.7", ".docx")]
    [InlineData("texto", ".pdf")]
    [InlineData("", ".pdf")]
    [InlineData("%PDF-1.7", ".exe")]
    public void Rechaza_firma_inexistente_o_que_no_coincide_con_la_extension(string contenido, string extension)
    {
        using var flujo = new MemoryStream(Encoding.ASCII.GetBytes(contenido));

        Assert.Null(TiposDeArchivo.Detectar(flujo, extension));
    }

    [Fact]
    [Trait("AC", "7.1")]
    public void Rechaza_un_zip_que_no_es_docx_o_esta_corrupto()
    {
        using var sinDocumento = Docx("[Content_Types].xml", "otra/cosa.xml");
        using var corrupto = new MemoryStream([0x50, 0x4B, 0x03, 0x04, 0x00, 0x01, 0x02]);

        Assert.Null(TiposDeArchivo.Detectar(sinDocumento, ".docx"));
        Assert.Null(TiposDeArchivo.Detectar(corrupto, ".docx"));
    }
}
