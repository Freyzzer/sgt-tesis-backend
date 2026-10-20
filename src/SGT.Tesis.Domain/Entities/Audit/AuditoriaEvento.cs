using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Domain.Entities.Audit;

/// <summary>
/// Represents an audit log event recording system actions and entity changes.
/// </summary>
public class AuditoriaEvento : BaseEntity<long>
{
    /// <summary>
    /// Gets or sets the unique identifier of the user who performed the action, if applicable.
    /// </summary>
    public int? UsuarioId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user who performed the action.
    /// </summary>
    public Usuario? Usuario { get; set; }

    /// <summary>
    /// Gets or sets the action performed during the audit event.
    /// </summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the entity affected by the event.
    /// </summary>
    public string EntidadNombre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary key identifier of the affected entity as a string representation.
    /// </summary>
    public string? EntidadId { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the entity state prior to the action.
    /// </summary>
    public string? ValoresAnterioresJSON { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the entity state after the action.
    /// </summary>
    public string? ValoresNuevosJSON { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action originated.
    /// </summary>
    public string? DireccionIp { get; set; }

    /// <summary>
    /// Gets or sets the HTTP user agent header string of the client performing the action.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the unique correlation identifier for tracing request pipelines across events.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the audit event occurred.
    /// </summary>
    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
}
