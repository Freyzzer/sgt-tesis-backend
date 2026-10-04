using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Security;

public class SesionUsuario : BaseEntity<Guid>
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public bool EsRevocado { get; set; } = false;
    public string? DireccionIp { get; set; }
    public string? UserAgent { get; set; }
}
