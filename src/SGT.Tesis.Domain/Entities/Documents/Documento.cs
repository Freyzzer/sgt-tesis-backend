using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Documents;

/// <summary>
/// Represents a thesis project document artifact managed by the system.
/// </summary>
public class Documento : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the thesis project to which this document belongs.
    /// </summary>
    public int ProyectoTesisId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the associated thesis project.
    /// </summary>
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type or category of the document (e.g., Proposal, Draft, Final Thesis).
    /// </summary>
    public string TipoDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description or notes about the document.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Gets or sets the current workflow status of the document.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the academic or administrative stage to which this document corresponds.
    /// </summary>
    public string Etapa { get; set; } = string.Empty;

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of file versions associated with this document.
    /// </summary>
    public ICollection<VersionDocumento> Versiones { get; set; } = new List<VersionDocumento>();
}
