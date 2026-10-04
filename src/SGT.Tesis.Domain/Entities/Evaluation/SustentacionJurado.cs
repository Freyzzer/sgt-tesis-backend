using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Domain.Entities.Evaluation;

public class SustentacionJurado : BaseEntity
{
    public int SustentacionId { get; set; }
    public Sustentacion Sustentacion { get; set; } = null!;

    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;

    public string RolJurado { get; set; } = string.Empty;
    public bool Asistio { get; set; } = false;
    public bool Confirmado { get; set; } = false;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
