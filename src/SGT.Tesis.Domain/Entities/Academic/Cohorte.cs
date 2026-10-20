using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

/// <summary>
/// Represents an academic cohort within an educational program.
/// </summary>
public class Cohorte : BaseEntity
{
    /// <summary>
    /// Gets or sets the cohort name.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the academic period associated with the cohort.
    /// </summary>
    public int Periodo { get; set; }

    /// <summary>
    /// Gets or sets the start date of the cohort.
    /// </summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>
    /// Gets or sets the end date of the cohort, if applicable.
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated academic program.
    /// </summary>
    public int ProgramaId { get; set; }

    /// <summary>
    /// Gets or sets the associated academic program.
    /// </summary>
    public Programa Programa { get; set; } = null!;

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of students enrolled in this cohort.
    /// </summary>
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
