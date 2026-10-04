using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Evaluation;

public class SolicitudDistincion : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public int? SustentacionId { get; set; }
    public Sustentacion? Sustentacion { get; set; }

    public string TipoDistincion { get; set; } = string.Empty;
    public string Justificacion { get; set; } = string.Empty;

    public int SolicitadoPorId { get; set; }
    public Usuario SolicitadoPor { get; set; } = null!;

    public string Estado { get; set; } = string.Empty;
    public bool AprobadoPorConsejo { get; set; } = false;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaResolucion { get; set; }
    public string? NumeroResolucion { get; set; }
}
