using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Projects;

/// <summary>
/// Represents the relationship entity between a thesis project and an assigned tutor.
/// </summary>
public class ProyectoTutor : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the thesis project.
    /// </summary>
    public int ProyectoTesisId { get; set; }

    /// <summary>
    /// Gets or sets the associated thesis project entity.
    /// </summary>
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the teacher acting as tutor.
    /// </summary>
    public int DocenteId { get; set; }

    /// <summary>
    /// Gets or sets the associated teacher entity.
    /// </summary>
    public Docente Docente { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role assigned to the tutor.
    /// </summary>
    public string RolTutor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the tutor was assigned to the project.
    /// </summary>
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the tutor was removed or retired from the project.
    /// </summary>
    public DateTime? FechaRetiro { get; set; }
}
