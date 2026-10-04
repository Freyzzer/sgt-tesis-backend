using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

public class Sustentacion : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public DateOnly FechaSustentacion { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public decimal? CalificacionNumerica { get; set; }
    public string? Resultado { get; set; }
    public string? ActaNumero { get; set; }
    public string? Observaciones { get; set; }

    // Propiedades de navegación
    public ICollection<SustentacionJurado> Jurados { get; set; } = new List<SustentacionJurado>();
    public ICollection<ResultadoEvaluacion> Resultados { get; set; } = new List<ResultadoEvaluacion>();
    public ICollection<SolicitudDistincion> SolicitudesDistincion { get; set; } = new List<SolicitudDistincion>();
}
