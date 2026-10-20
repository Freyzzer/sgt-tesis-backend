using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

/// <summary>
/// Represents the evaluation result submitted by an evaluator for a thesis project or defense session.
/// </summary>
public class ResultadoEvaluacion : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the evaluated thesis project.
    /// </summary>
    public int ProyectoTesisId { get; set; }

    /// <summary>
    /// Gets or sets the evaluated thesis project navigation entity.
    /// </summary>
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the defense session (sustentación), if applicable.
    /// </summary>
    public int? SustentacionId { get; set; }

    /// <summary>
    /// Gets or sets the defense session navigation entity, if applicable.
    /// </summary>
    public Sustentacion? Sustentacion { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the faculty evaluator.
    /// </summary>
    public int EvaluadorId { get; set; }

    /// <summary>
    /// Gets or sets the faculty evaluator navigation entity.
    /// </summary>
    public Docente Evaluador { get; set; } = null!;

    /// <summary>
    /// Gets or sets the numerical grade or score assigned by the evaluator.
    /// </summary>
    public decimal Calificacion { get; set; }

    /// <summary>
    /// Gets or sets the evaluation criteria breakdown formatted as a JSON string.
    /// </summary>
    public string? CriteriosJSON { get; set; }

    /// <summary>
    /// Gets or sets the overall evaluation verdict or concept (e.g., Approved, Rejected, Revision Required).
    /// </summary>
    public string Concepto { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional comments or feedback provided by the evaluator.
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the evaluation was completed.
    /// </summary>
    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
}
