using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Documents;

public class DocumentoLegal : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;
    public bool EsObligatorio { get; set; } = true;

    // Propiedades de navegación
    public ICollection<ConsentimientoUsuario> Consentimientos { get; set; } = new List<ConsentimientoUsuario>();
}
