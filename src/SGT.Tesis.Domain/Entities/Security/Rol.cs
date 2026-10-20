using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

/// <summary>
/// Represents a security role entity used for access control within the system.
/// </summary>
public class Rol : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Descripcion { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of user roles associated with this role.
    /// </summary>
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
