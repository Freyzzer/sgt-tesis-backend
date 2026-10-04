using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Documents;

public class Revision : BaseEntity
{
    public Guid VersionDocumentoId { get; set; }
    public VersionDocumento VersionDocumento { get; set; } = null!;

    public int RevisorId { get; set; }
    public Usuario Revisor { get; set; } = null!;

    public string EstadoRevision { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string? RutaArchivoCorrecciones { get; set; }
    public DateTime? FechaRevision { get; set; }
}
