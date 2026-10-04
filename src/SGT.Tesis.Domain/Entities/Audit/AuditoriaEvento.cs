using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Audit;

public class AuditoriaEvento : BaseEntity<long>
{
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Accion { get; set; } = string.Empty;
    public string EntidadNombre { get; set; } = string.Empty;
    public string? EntidadId { get; set; }

    public string? ValoresAnterioresJSON { get; set; }
    public string? ValoresNuevosJSON { get; set; }

    public string? DireccionIp { get; set; }
    public string? UserAgent { get; set; }

    public Guid CorrelationId { get; set; }
    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
}
