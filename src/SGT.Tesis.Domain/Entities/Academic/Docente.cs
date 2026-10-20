using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Academic;

/// <summary>
/// Represents a professor or academic faculty member.
/// </summary>
public class Docente : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique code assigned to the professor.
    /// </summary>
    public string CodigoDocente { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the area of specialization of the professor.
    /// </summary>
    public string? Especialidad { get; set; }

    /// <summary>
    /// Gets or sets the academic degree or title held by the professor.
    /// </summary>
    public string? TituloAcademico { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated user account.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the associated user entity.
    /// </summary>
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the associated research line, if any.
    /// </summary>
    public int? LineaInvestigacionId { get; set; }

    /// <summary>
    /// Gets or sets the associated research line entity.
    /// </summary>
    public LineaInvestigacion? LineaInvestigacion { get; set; }
}
