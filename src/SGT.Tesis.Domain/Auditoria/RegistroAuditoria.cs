using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Auditoria;

/// <summary>
/// Registro de una acción relevante (quién, qué, sobre qué, cuándo, desde dónde). Es inmutable:
/// no tiene mutadores y la persistencia rechaza actualizarlo o borrarlo.
/// </summary>
public sealed class RegistroAuditoria : BaseEntity
{
    public const int AccionMaxLength = 100;
    public const int EntidadMaxLength = 100;
    public const int IpMaxLength = 45;
    public const int CorrelacionMaxLength = 64;
    public const int DetalleMaxLength = 1000;

    private RegistroAuditoria()
    {
        Accion = string.Empty;
        Entidad = string.Empty;
    }

    /// <summary>Quien ejecutó la acción; vacío si no hay un usuario identificado (por ejemplo, un correo desconocido en el login).</summary>
    public Guid? ActorId { get; private set; }

    public string Accion { get; private set; }

    public string Entidad { get; private set; }

    public Guid? EntidadId { get; private set; }

    /// <summary>Tesis a la que pertenece el evento, para consultar el historial de una tesis.</summary>
    public Guid? TesisId { get; private set; }

    public DateTime FechaUtc { get; private set; }

    public string? Ip { get; private set; }

    public string? CorrelacionId { get; private set; }

    public string? Detalle { get; private set; }

    public static RegistroAuditoria Crear(
        string? accion,
        string? entidad,
        Guid? actorId,
        Guid? entidadId,
        Guid? tesisId,
        string? detalle,
        string? ip,
        string? correlacionId,
        DateTime utcNow) => new()
        {
            Accion = Guard.Required(accion, AccionMaxLength, nameof(Accion)),
            Entidad = Guard.Required(entidad, EntidadMaxLength, nameof(Entidad)),
            ActorId = actorId == Guid.Empty ? null : actorId,
            EntidadId = entidadId == Guid.Empty ? null : entidadId,
            TesisId = tesisId == Guid.Empty ? null : tesisId,
            Detalle = Recortar(detalle, DetalleMaxLength),
            Ip = Recortar(ip, IpMaxLength),
            CorrelacionId = Recortar(correlacionId, CorrelacionMaxLength),
            FechaUtc = utcNow,
        };

    private static string? Recortar(string? valor, int maximo)
    {
        var texto = valor?.Trim();

        if (string.IsNullOrEmpty(texto))
        {
            return null;
        }

        return texto.Length > maximo ? texto[..maximo] : texto;
    }
}
