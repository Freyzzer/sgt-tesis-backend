using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

/// <summary>
/// Represents a user session entity used to manage refresh tokens and session security.
/// </summary>
public class SesionUsuario : BaseEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier of the associated user.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the associated user entity.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the refresh token string for authentication renewal.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date and time of the refresh token.
    /// </summary>
    public DateTime FechaExpiracion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session token has been revoked.
    /// </summary>
    public bool EsRevocado { get; set; } = false;

    /// <summary>
    /// Gets or sets the IP address from which the session was established.
    /// </summary>
    public string? DireccionIp { get; set; }

    /// <summary>
    /// Gets or sets the client User-Agent string associated with the session.
    /// </summary>
    public string? UserAgent { get; set; }
}
