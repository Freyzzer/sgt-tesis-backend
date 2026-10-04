using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Documents;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Domain.Entities.Projects;

/// <summary>
/// Represents a thesis project entity within the system.
/// </summary>
public class ProyectoTesis : BaseEntity
{
    /// <summary>
    /// Gets or sets the title of the thesis project.
    /// </summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary or abstract of the thesis project.
    /// </summary>
    public string? Resumen { get; set; }

    /// <summary>
    /// Gets or sets the modality of the thesis project.
    /// </summary>
    public string Modalidad { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the thesis project.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the thesis project was formulated.
    /// </summary>
    public DateOnly FechaFormulacion { get; set; }

    /// <summary>
    /// Gets or sets the start date of the thesis project.
    /// </summary>
    public DateOnly? FechaInicio { get; set; }

    /// <summary>
    /// Gets or sets the end date of the thesis project.
    /// </summary>
    public DateOnly? FechaFin { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the thesis project was approved.
    /// </summary>
    public DateTime? FechaAprobacion { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the associated student.
    /// </summary>
    public int EstudianteId { get; set; }

    /// <summary>
    /// Gets or sets the associated student entity.
    /// </summary>
    public Estudiante Estudiante { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the research line.
    /// </summary>
    public int LineaInvestigacionId { get; set; }

    /// <summary>
    /// Gets or sets the associated research line entity.
    /// </summary>
    public LineaInvestigacion LineaInvestigacion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the thesis director.
    /// </summary>
    public int? DirectorId { get; set; }

    /// <summary>
    /// Gets or sets the thesis director entity.
    /// </summary>
    public Docente? Director { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the thesis co-director.
    /// </summary>
    public int? CodirectorId { get; set; }

    /// <summary>
    /// Gets or sets the thesis co-director entity.
    /// </summary>
    public Docente? Codirector { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the collection of tutors assigned to the project.
    /// </summary>
    public ICollection<ProyectoTutor> Tutores { get; set; } = new List<ProyectoTutor>();

    /// <summary>
    /// Gets or sets the collection of documents associated with the thesis project.
    /// </summary>
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();

    /// <summary>
    /// Gets or sets the collection of defense sessions (sustentaciones) for the thesis project.
    /// </summary>
    public ICollection<Sustentacion> Sustentaciones { get; set; } = new List<Sustentacion>();

    /// <summary>
    /// Gets or sets the collection of evaluation results for the thesis project.
    /// </summary>
    public ICollection<ResultadoEvaluacion> ResultadosEvaluacion { get; set; } = new List<ResultadoEvaluacion>();

    /// <summary>
    /// Gets or sets the collection of distinction requests for the thesis project.
    /// </summary>
    public ICollection<SolicitudDistincion> SolicitudesDistincion { get; set; } = new List<SolicitudDistincion>();

    /// <summary>
    /// Gets or sets the collection of publications associated with the thesis project.
    /// </summary>
    public ICollection<Publicacion> Publicaciones { get; set; } = new List<Publicacion>();
}
