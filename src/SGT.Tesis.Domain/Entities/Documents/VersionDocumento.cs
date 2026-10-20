using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

/// <summary>
/// Represents a specific file version of a thesis document stored in MinIO storage.
/// </summary>
public class VersionDocumento : BaseEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier of the parent document.
    /// </summary>
    public int DocumentoId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the parent document.
    /// </summary>
    public Documento Documento { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sequential version number of the document file.
    /// </summary>
    public int NumeroVersion { get; set; }

    /// <summary>
    /// Gets or sets the original file name as uploaded by the user.
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MinIO bucket name where the file binary is stored.
    /// </summary>
    public string BucketMinio { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique MinIO object key within the bucket.
    /// </summary>
    public string ObjectKeyMinio { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full MinIO storage reference path.
    /// </summary>
    public string RutaMinio { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extension (e.g., ".pdf", ".docx").
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long TamanoBytes { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic checksum hash (e.g., SHA-256) of the file content.
    /// </summary>
    public string HashArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME media type of the file content.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the user who uploaded this version.
    /// </summary>
    public int SubidoPorId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user who uploaded this version.
    /// </summary>
    public Usuario SubidoPor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC timestamp when the file version was uploaded.
    /// </summary>
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets optional user comments or revision notes associated with this upload.
    /// </summary>
    public string? Comentarios { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of reviews conducted on this document version.
    /// </summary>
    public ICollection<Revision> Revisiones { get; set; } = new List<Revision>();
}
