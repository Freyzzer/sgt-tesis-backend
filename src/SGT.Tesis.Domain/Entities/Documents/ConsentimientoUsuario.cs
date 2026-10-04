using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

/// <summary>
/// Represents a user's explicit consent to a specific legal document version (terms of service, privacy policy, etc.).
/// </summary>
public class ConsentimientoUsuario : BaseEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier of the user granting consent.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user granting consent.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the legal document being consented to.
    /// </summary>
    public int DocumentoLegalId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated legal document.
    /// </summary>
    public DocumentoLegal DocumentoLegal { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the legal document was accepted by the user.
    /// </summary>
    public bool Aceptado { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user accepted the legal document.
    /// </summary>
    public DateTime FechaAceptacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the IP address from which the consent was registered.
    /// </summary>
    public string? DireccionIp { get; set; }

    /// <summary>
    /// Gets or sets the client user agent string recorded at the time of consent.
    /// </summary>
    public string? UserAgent { get; set; }
}
