using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Academic;

/// <summary>
/// Represents a student domain entity within the academic system.
/// </summary>
public class Estudiante : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique student identification code.
    /// </summary>
    public string CodigoEstudiantil { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current academic status of the student.
    /// </summary>
    public string? EstadoAcademico { get; set; }

    /// <summary>
    /// Gets or sets the foreign key for the associated user account.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated user account.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the foreign key for the associated academic program.
    /// </summary>
    public int ProgramaId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated academic program.
    /// </summary>
    public Programa Programa { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional foreign key for the associated cohort.
    /// </summary>
    public int? CohorteId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated cohort.
    /// </summary>
    public Cohorte? Cohorte { get; set; }
}
