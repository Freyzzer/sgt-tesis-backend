using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Documents;

public class Documento : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public string TipoDocumento { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Etapa { get; set; } = string.Empty;

    // Propiedades de navegación
    public ICollection<VersionDocumento> Versiones { get; set; } = new List<VersionDocumento>();
}
