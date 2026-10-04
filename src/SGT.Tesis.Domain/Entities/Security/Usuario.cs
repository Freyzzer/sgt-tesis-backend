using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Security;

public class Usuario : BaseEntity
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    // Propiedades de navegación
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<SesionUsuario> Sesiones { get; set; } = new List<SesionUsuario>();
    public Estudiante? Estudiante { get; set; }
    public Docente? Docente { get; set; }
}
