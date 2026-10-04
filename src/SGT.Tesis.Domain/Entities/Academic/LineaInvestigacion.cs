using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

public class LineaInvestigacion : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int ProgramaId { get; set; }
    public Programa Programa { get; set; } = null!;

    // Propiedades de navegación
    public ICollection<Docente> Docentes { get; set; } = new List<Docente>();
}
