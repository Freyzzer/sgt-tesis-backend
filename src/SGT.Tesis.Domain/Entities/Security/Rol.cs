using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

public class Rol : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // Propiedades de navegación
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
