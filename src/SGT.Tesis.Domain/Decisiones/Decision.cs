using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Decisiones;

public enum ResultadoDecision
{
    Aprobada = 1,
    Observada = 2,
    Rechazada = 3,
}

/// <summary>
/// Decisión del Administrador sobre una etapa. Es inmutable: no tiene mutadores y la capa de persistencia
/// rechaza cualquier actualización o borrado.
/// </summary>
public sealed class Decision : BaseEntity
{
    public const int JustificacionMaxLength = 2000;

    private Decision()
    {
    }

    public Guid EtapaId { get; private set; }

    public Guid AdministradorId { get; private set; }

    public ResultadoDecision Resultado { get; private set; }

    /// <summary>Obligatoria al observar o rechazar; opcional al aprobar.</summary>
    public string? Justificacion { get; private set; }

    public DateTime FechaUtc { get; private set; }

    public static Decision Crear(Guid etapaId, Guid administradorId, ResultadoDecision resultado, string? justificacion, DateTime utcNow)
    {
        if (!Enum.IsDefined(resultado))
        {
            throw new DomainException("El resultado de la decisión no es válido.");
        }

        var texto = justificacion?.Trim();

        if (resultado != ResultadoDecision.Aprobada && string.IsNullOrEmpty(texto))
        {
            throw new DomainException("La justificación es obligatoria al observar o rechazar una etapa.");
        }

        if (texto is { Length: > JustificacionMaxLength })
        {
            throw new DomainException($"La justificación no puede superar {JustificacionMaxLength} caracteres.");
        }

        return new Decision
        {
            EtapaId = Guard.NotEmpty(etapaId, nameof(EtapaId)),
            AdministradorId = Guard.NotEmpty(administradorId, nameof(AdministradorId)),
            Resultado = resultado,
            Justificacion = string.IsNullOrEmpty(texto) ? null : texto,
            FechaUtc = utcNow,
        };
    }
}
