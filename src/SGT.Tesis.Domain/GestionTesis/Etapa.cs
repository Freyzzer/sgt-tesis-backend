using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;

namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>Una de las 7 etapas de una tesis. Sus transiciones se ejecutan a través de <see cref="TesisGrado"/>.</summary>
public sealed class Etapa : BaseEntity
{
    private Etapa()
    {
    }

    public Guid TesisId { get; private set; }

    public EtapaTipo Tipo { get; private set; }

    public EstadoEtapa Estado { get; private set; }

    public DateTime FechaApertura { get; private set; }

    public DateTime? FechaCierre { get; private set; }

    /// <summary>Momento del último envío a revisión; permite medir cuánto lleva esperando una validación.</summary>
    public DateTime? EnRevisionDesde { get; private set; }

    /// <summary>
    /// Ronda de evaluación actual: cada envío a revisión abre una nueva (la primera es la 1; 0 mientras no se ha enviado).
    /// Los veredictos y las decisiones pertenecen a una ronda y solo los de la actual cuentan para aprobar (ADR-005).
    /// </summary>
    public int Ronda { get; private set; }

    /// <summary>Contador de cambios de estado; se usa como token de concurrencia optimista.</summary>
    public int Revision { get; private set; }

    /// <summary>Una etapa está abierta mientras no haya sido aprobada ni rechazada (un rechazo reintentable se reabre al reenviar).</summary>
    public bool EstaAbierta => Estado is EstadoEtapa.Pendiente or EstadoEtapa.EnRevision or EstadoEtapa.Observado;

    /// <summary>
    /// Se pueden cargar documentos mientras la etapa está Pendiente u Observada, o Rechazada. En este último caso solo si la
    /// tesis sigue en curso (rechazo reintentable); <see cref="TesisGrado"/> lo garantiza.
    /// </summary>
    public bool AdmiteDocumentos => Estado is EstadoEtapa.Pendiente or EstadoEtapa.Observado or EstadoEtapa.Rechazado;

    internal static Etapa Abrir(Guid tesisId, EtapaTipo tipo, DateTime utcNow) => new()
    {
        TesisId = tesisId,
        Tipo = Enum.IsDefined(tipo) ? tipo : throw new DomainException("El tipo de etapa no es válido."),
        Estado = EstadoEtapa.Pendiente,
        FechaApertura = utcNow,
    };

    /// <summary>Pendiente, Observado o Rechazado (reintento) → EnRevision.</summary>
    internal void EnviarARevision(DateTime utcNow)
    {
        if (Estado is not (EstadoEtapa.Pendiente or EstadoEtapa.Observado or EstadoEtapa.Rechazado))
        {
            throw new TransicionInvalidaException(Estado == EstadoEtapa.EnRevision
                ? "La etapa ya está en revisión."
                : $"La etapa está {Estado} y no admite nuevos envíos.");
        }

        Estado = EstadoEtapa.EnRevision;
        EnRevisionDesde = utcNow;
        Ronda++;
        Revision++;
    }

    /// <summary>Rechazado → Observado: el administrador devuelve la etapa al estudiante para que la corrija y reenvíe.</summary>
    internal void Reabrir()
    {
        if (Estado != EstadoEtapa.Rechazado)
        {
            throw new TransicionInvalidaException("Solo se puede reabrir una etapa rechazada.");
        }

        Estado = EstadoEtapa.Observado;
        FechaCierre = null;
        Revision++;
    }

    /// <summary>
    /// EnRevision → Aprobado, Observado o Rechazado, según la decisión del Administrador. Un rechazo reintentable
    /// (<paramref name="reintentable"/>) no cierra la etapa: el estudiante puede corregir y reenviar.
    /// </summary>
    internal void Resolver(ResultadoDecision resultado, DateTime utcNow, bool reintentable = false)
    {
        if (Estado != EstadoEtapa.EnRevision)
        {
            throw new TransicionInvalidaException(Estado is EstadoEtapa.Aprobado or EstadoEtapa.Rechazado
                ? $"La etapa ya está {Estado}."
                : "Solo se puede decidir sobre una etapa que está en revisión.");
        }

        Estado = resultado switch
        {
            ResultadoDecision.Aprobada => EstadoEtapa.Aprobado,
            ResultadoDecision.Observada => EstadoEtapa.Observado,
            ResultadoDecision.Rechazada => EstadoEtapa.Rechazado,
            _ => throw new DomainException("El resultado de la decisión no es válido."),
        };

        if (Estado == EstadoEtapa.Aprobado || (Estado == EstadoEtapa.Rechazado && !reintentable))
        {
            FechaCierre = utcNow;
        }

        Revision++;
    }
}
