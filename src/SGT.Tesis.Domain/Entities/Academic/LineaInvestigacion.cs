using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

/// <summary>
/// Represents a research line domain entity within an academic program.
/// </summary>
public class LineaInvestigacion : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique code of the research line.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the research line.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the research line.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Gets or sets the foreign key for the associated academic program.
    /// </summary>
    public int ProgramaId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated academic program.
    /// </summary>
    public Programa Programa { get; set; } = null!;

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of professors associated with this research line.
    /// </summary>
    public ICollection<Docente> Docentes { get; set; } = new List<Docente>();
}
