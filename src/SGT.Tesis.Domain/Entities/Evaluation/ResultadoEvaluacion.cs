using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

public class ResultadoEvaluacion : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public int? SustentacionId { get; set; }
    public Sustentacion? Sustentacion { get; set; }

    public int EvaluadorId { get; set; }
    public Docente Evaluador { get; set; } = null!;

    public decimal Calificacion { get; set; }
    public string? CriteriosJSON { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
}
