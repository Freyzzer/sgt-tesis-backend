using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Evaluation;

/// <summary>
/// Represents a formal request for an academic distinction or honor for an outstanding thesis project.
/// </summary>
public class SolicitudDistincion : BaseEntity
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
    /// Gets or sets the unique identifier of the defense session (sustentación), if applicable.
    /// </summary>
    public int? SustentacionId { get; set; }

    /// <summary>
    /// Gets or sets the defense session navigation entity, if applicable.
    /// </summary>
    public Sustentacion? Sustentacion { get; set; }

    /// <summary>
    /// Gets or sets the requested type of distinction (e.g., Cum Laude, Magna Cum Laude, Summa Cum Laude).
    /// </summary>
    public string TipoDistincion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the justification rationale for requesting the distinction.
    /// </summary>
    public string Justificacion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the user who submitted the request.
    /// </summary>
    public int SolicitadoPorId { get; set; }

    /// <summary>
    /// Gets or sets the requesting user navigation entity.
    /// </summary>
    public Usuario SolicitadoPor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the request (e.g., Pending, Approved, Rejected).
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the request has been approved by the department council.
    /// </summary>
    public bool AprobadoPorConsejo { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the request was submitted.
    /// </summary>
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the resolution date when a decision was rendered by the council, if available.
    /// </summary>
    public DateTime? FechaResolucion { get; set; }

    /// <summary>
    /// Gets or sets the official resolution document number, if available.
    /// </summary>
    public string? NumeroResolucion { get; set; }
}
