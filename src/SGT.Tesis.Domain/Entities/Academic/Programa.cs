using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

/// <summary>
/// Represents an academic program domain entity.
/// </summary>
public class Programa : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique code of the academic program.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the academic program.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the academic program.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Gets or sets the educational level of the academic program.
    /// </summary>
    public string? NivelFormacion { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of cohorts associated with this academic program.
    /// </summary>
    public ICollection<Cohorte> Cohortes { get; set; } = new List<Cohorte>();

    /// <summary>
    /// Gets or sets the collection of research lines associated with this academic program.
    /// </summary>
    public ICollection<LineaInvestigacion> LineasInvestigacion { get; set; } = new List<LineaInvestigacion>();

    /// <summary>
    /// Gets or sets the collection of students enrolled in this academic program.
    /// </summary>
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
