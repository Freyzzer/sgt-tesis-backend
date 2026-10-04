using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Academic;

public class Estudiante : BaseEntity
{
    public string CodigoEstudiantil { get; set; } = string.Empty;
    public string? EstadoAcademico { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int ProgramaId { get; set; }
    public Programa Programa { get; set; } = null!;

    public int? CohorteId { get; set; }
    public Cohorte? Cohorte { get; set; }
}
