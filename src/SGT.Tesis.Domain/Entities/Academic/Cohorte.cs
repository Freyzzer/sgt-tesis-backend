using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Entities.Academic;

public class Cohorte : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public int Periodo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public int ProgramaId { get; set; }
    public Programa Programa { get; set; } = null!;

    // Propiedades de navegación
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
