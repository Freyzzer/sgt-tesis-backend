using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

/// <summary>
/// Represents a thesis defense session (sustentación) scheduled or completed for a thesis project.
/// </summary>
public class Sustentacion : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the associated thesis project.
    /// </summary>
    public int ProyectoTesisId { get; set; }

    /// <summary>
    /// Gets or sets the associated thesis project navigation entity.
    /// </summary>
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the scheduled date for the defense session.
    /// </summary>
    public DateOnly FechaSustentacion { get; set; }

    /// <summary>
    /// Gets or sets the scheduled start time of the defense session.
    /// </summary>
    public TimeOnly HoraInicio { get; set; }

    /// <summary>
    /// Gets or sets the scheduled or actual end time of the defense session.
    /// </summary>
    public TimeOnly? HoraFin { get; set; }

    /// <summary>
    /// Gets or sets the location or classroom where the defense session takes place.
    /// </summary>
    public string Lugar { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery mode of the defense session (e.g., In-Person, Virtual, Hybrid).
    /// </summary>
    public string Modalidad { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the defense session (e.g., Scheduled, In Progress, Completed, Cancelled).
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the final numerical score awarded for the thesis defense, if graded.
    /// </summary>
    public decimal? CalificacionNumerica { get; set; }

    /// <summary>
    /// Gets or sets the qualitative outcome or result of the defense (e.g., Approved, Failed, Conditional Approval).
    /// </summary>
    public string? Resultado { get; set; }

    /// <summary>
    /// Gets or sets the official minute or record identifier number for the defense session.
    /// </summary>
    public string? ActaNumero { get; set; }

    /// <summary>
    /// Gets or sets additional comments, conditions, or observations recorded during the defense.
    /// </summary>
    public string? Observaciones { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of jury members assigned to evaluate this defense session.
    /// </summary>
    public ICollection<SustentacionJurado> Jurados { get; set; } = new List<SustentacionJurado>();

    /// <summary>
    /// Gets or sets the collection of evaluation results recorded for this defense session.
    /// </summary>
    public ICollection<ResultadoEvaluacion> Resultados { get; set; } = new List<ResultadoEvaluacion>();

    /// <summary>
    /// Gets or sets the collection of distinction requests associated with this defense session.
    /// </summary>
    public ICollection<SolicitudDistincion> SolicitudesDistincion { get; set; } = new List<SolicitudDistincion>();
}
