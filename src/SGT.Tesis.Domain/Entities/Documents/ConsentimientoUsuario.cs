using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

public class ConsentimientoUsuario : BaseEntity<Guid>
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int DocumentoLegalId { get; set; }
    public DocumentoLegal DocumentoLegal { get; set; } = null!;

    public bool Aceptado { get; set; } = true;
    public DateTime FechaAceptacion { get; set; } = DateTime.UtcNow;
    public string? DireccionIp { get; set; }
    public string? UserAgent { get; set; }
}
