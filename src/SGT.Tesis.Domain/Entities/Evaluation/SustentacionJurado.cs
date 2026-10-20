using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Evaluation;

/// <summary>
/// Represents a faculty member assigned as a jury member to a specific thesis defense session (sustentación).
/// </summary>
public class SustentacionJurado : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the thesis defense session.
    /// </summary>
    public int SustentacionId { get; set; }

    /// <summary>
    /// Gets or sets the thesis defense session navigation entity.
    /// </summary>
    public Sustentacion Sustentacion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the assigned faculty member (docente).
    /// </summary>
    public int DocenteId { get; set; }

    /// <summary>
    /// Gets or sets the assigned faculty member navigation entity.
    /// </summary>
    public Docente Docente { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific role of the jury member during the defense (e.g., President, Secretary, Evaluator).
    /// </summary>
    public string RolJurado { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the jury member attended the defense session.
    /// </summary>
    public bool Asistio { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the jury member has confirmed their participation.
    /// </summary>
    public bool Confirmado { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the jury member was assigned to the defense session.
    /// </summary>
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
