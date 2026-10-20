using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Documents;

/// <summary>
/// Represents a legal document template or policy requiring user acceptance (e.g., terms of service, privacy policy).
/// </summary>
public class DocumentoLegal : BaseEntity
{
    /// <summary>
    /// Gets or sets the title of the legal document.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category or type of legal document.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full body content or text of the legal document.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version string of the legal document (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this legal document version was published.
    /// </summary>
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether user acceptance of this document is mandatory.
    /// </summary>
    public bool EsObligatorio { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of user consents registered for this legal document.
    /// </summary>
    public ICollection<ConsentimientoUsuario> Consentimientos { get; set; } = new List<ConsentimientoUsuario>();
}
