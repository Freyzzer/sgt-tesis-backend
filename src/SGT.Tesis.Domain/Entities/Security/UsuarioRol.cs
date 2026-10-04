using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

/// <summary>
/// Represents the relationship entity linking a user to an assigned role.
/// </summary>
public class UsuarioRol : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the associated user entity.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the role.
    /// </summary>
    public int RolId { get; set; }

    /// <summary>
    /// Gets or sets the associated role entity.
    /// </summary>
    public Rol Rol { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the role was assigned to the user.
    /// </summary>
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
