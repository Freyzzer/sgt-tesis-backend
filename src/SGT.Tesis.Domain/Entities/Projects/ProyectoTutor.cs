using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Projects;

public class ProyectoTutor : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;

    public string RolTutor { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRetiro { get; set; }
}
