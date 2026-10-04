using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Academic;

public class Docente : BaseEntity
{
    public string CodigoDocente { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
    public string? TituloAcademico { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int? LineaInvestigacionId { get; set; }
    public LineaInvestigacion? LineaInvestigacion { get; set; }
}
