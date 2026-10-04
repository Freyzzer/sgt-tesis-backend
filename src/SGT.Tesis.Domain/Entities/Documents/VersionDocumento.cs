using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

public class VersionDocumento : BaseEntity<Guid>
{
    public int DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;

    public int NumeroVersion { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string BucketMinio { get; set; } = string.Empty;
    public string ObjectKeyMinio { get; set; } = string.Empty;
    public string RutaMinio { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string HashArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public int SubidoPorId { get; set; }
    public Usuario SubidoPor { get; set; } = null!;

    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    public string? Comentarios { get; set; }

    // Propiedades de navegación
    public ICollection<Revision> Revisiones { get; set; } = new List<Revision>();
}
