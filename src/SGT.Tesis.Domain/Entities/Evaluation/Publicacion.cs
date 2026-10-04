using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Domain.Entities.Evaluation;

public class Publicacion : BaseEntity
{
    public int ProyectoTesisId { get; set; }
    public ProyectoTesis ProyectoTesis { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string NombreRevistaOEvento { get; set; } = string.Empty;
    public string TipoPublicacion { get; set; } = string.Empty;
    public string? Indexacion { get; set; }
    public string? Doi { get; set; }
    public string? Url { get; set; }
    public DateOnly? FechaPublicacion { get; set; }
    public string Estado { get; set; } = string.Empty;
}
