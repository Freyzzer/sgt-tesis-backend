using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

public class UsuarioRol : BaseEntity
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
