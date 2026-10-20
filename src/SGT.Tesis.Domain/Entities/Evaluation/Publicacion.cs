using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

/// <summary>
/// Represents an academic or scientific publication associated with a thesis project.
/// </summary>
public class Publicacion : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the associated thesis project.
    /// </summary>
    public int ProyectoTesisId { get; set; }

    /// <summary>
    /// Gets or sets the associated thesis project.
    /// </summary>
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title of the publication.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the journal or conference event where the work was published.
    /// </summary>
    public string NombreRevistaOEvento { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of publication (e.g., Journal Article, Conference Paper).
    /// </summary>
    public string TipoPublicacion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the indexing database or system (e.g., Scopus, Web of Science, Scielo).
    /// </summary>
    public string? Indexacion { get; set; }

    /// <summary>
    /// Gets or sets the Digital Object Identifier (DOI) of the publication.
    /// </summary>
    public string? Doi { get; set; }

    /// <summary>
    /// Gets or sets the web URL to the publication.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the date when the publication was released or accepted.
    /// </summary>
    public DateOnly? FechaPublicacion { get; set; }

    /// <summary>
    /// Gets or sets the current status of the publication (e.g., Submitted, Accepted, Published).
    /// </summary>
    public string Estado { get; set; } = string.Empty;
}
