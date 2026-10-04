using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Security;

/// <summary>
/// Represents a user entity within the authentication and security domain.
/// </summary>
public class Usuario : BaseEntity
{
    /// <summary>
    /// Gets or sets the first name(s) of the user.
    /// </summary>
    public string Nombres { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name(s) of the user.
    /// </summary>
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity document number of the user.
    /// </summary>
    public string DocumentoIdentidad { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the telephone number of the user.
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user last accessed the system.
    /// </summary>
    public DateTime? UltimoAcceso { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of user roles assigned to this user.
    /// </summary>
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();

    /// <summary>
    /// Gets or sets the collection of sessions belonging to this user.
    /// </summary>
    public ICollection<SesionUsuario> Sesiones { get; set; } = new List<SesionUsuario>();

    /// <summary>
    /// Gets or sets the associated student entity, if applicable.
    /// </summary>
    public Estudiante? Estudiante { get; set; }

    /// <summary>
    /// Gets or sets the associated teacher entity, if applicable.
    /// </summary>
    public Docente? Docente { get; set; }
}
