using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

public class Programa : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? NivelFormacion { get; set; }

    // Propiedades de navegación
    public ICollection<Cohorte> Cohortes { get; set; } = new List<Cohorte>();
    public ICollection<LineaInvestigacion> LineasInvestigacion { get; set; } = new List<LineaInvestigacion>();
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
