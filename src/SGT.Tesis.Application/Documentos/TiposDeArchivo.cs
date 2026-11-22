using System.IO.Compression;

namespace SGT.Tesis.Application.Documentos;

/// <summary>Valida el tipo real de un archivo por su contenido (firma), no por la extensión declarada.</summary>
public static class TiposDeArchivo
{
    public const string Pdf = "application/pdf";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();
    private static readonly byte[] FirmaZip = [0x50, 0x4B, 0x03, 0x04];

    public static string? ExtensionPara(string contentType) => contentType switch
    {
        Pdf => ".pdf",
        Docx => ".docx",
        _ => null,
    };

    /// <summary>
    /// Devuelve el tipo de contenido si el flujo es un PDF o un DOCX válido y la extensión coincide;
    /// <c>null</c> en caso contrario. El flujo debe ser seekable; se deja en la posición 0.
    /// </summary>
    public static string? Detectar(Stream contenido, string extension)
    {
        contenido.Position = 0;

        try
        {
            var encabezado = new byte[8];
            var leidos = contenido.Read(encabezado, 0, encabezado.Length);
            contenido.Position = 0;

            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                && leidos >= FirmaPdf.Length && encabezado.AsSpan(0, FirmaPdf.Length).SequenceEqual(FirmaPdf))
            {
                return Pdf;
            }

            if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
                && leidos >= FirmaZip.Length && encabezado.AsSpan(0, FirmaZip.Length).SequenceEqual(FirmaZip)
                && EsDocx(contenido))
            {
                return Docx;
            }

            return null;
        }
        finally
        {
            contenido.Position = 0;
        }
    }

    private static bool EsDocx(Stream contenido)
    {
        try
        {
            using var zip = new ZipArchive(contenido, ZipArchiveMode.Read, leaveOpen: true);

            return zip.GetEntry("[Content_Types].xml") is not null && zip.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
