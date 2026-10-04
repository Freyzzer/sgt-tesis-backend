using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Documents;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Domain.Entities.Projects;

public class ProyectoTesis : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Resumen { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public DateOnly FechaFormulacion { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public DateTime? FechaAprobacion { get; set; }

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public int LineaInvestigacionId { get; set; }
    public LineaInvestigacion LineaInvestigacion { get; set; } = null!;

    public int? DirectorId { get; set; }
    public Docente? Director { get; set; }

    public int? CodirectorId { get; set; }
    public Docente? Codirector { get; set; }

    // Propiedades de navegación
    public ICollection<ProyectoTutor> Tutores { get; set; } = new List<ProyectoTutor>();
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
    public ICollection<Sustentacion> Sustentaciones { get; set; } = new List<Sustentacion>();
    public ICollection<ResultadoEvaluacion> ResultadosEvaluacion { get; set; } = new List<ResultadoEvaluacion>();
    public ICollection<SolicitudDistincion> SolicitudesDistincion { get; set; } = new List<SolicitudDistincion>();
    public ICollection<Publicacion> Publicaciones { get; set; } = new List<Publicacion>();
}
