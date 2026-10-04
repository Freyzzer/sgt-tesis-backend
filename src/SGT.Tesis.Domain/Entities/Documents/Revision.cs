using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

/// <summary>
/// Represents an evaluation or review record submitted by a reviewer for a specific document version.
/// </summary>
public class Revision : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the document version being reviewed.
    /// </summary>
    public Guid VersionDocumentoId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the target document version.
    /// </summary>
    public VersionDocumento VersionDocumento { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the reviewer user.
    /// </summary>
    public int RevisorId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the reviewer user.
    /// </summary>
    public Usuario Revisor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the review (e.g., Approved, Rejected, Changes Requested).
    /// </summary>
    public string EstadoRevision { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets detailed remarks, comments, or feedback provided by the reviewer.
    /// </summary>
    public string Observaciones { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional file storage path or object key for attached corrected document files.
    /// </summary>
    public string? RutaArchivoCorrecciones { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the review was completed, or null if pending.
    /// </summary>
    public DateTime? FechaRevision { get; set; }
}
