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

    /// <summary>Contador de cambios de estado; se usa como token de concurrencia optimista.</summary>
    public int Revision { get; private set; }

    /// <summary>Una etapa está abierta mientras no haya sido aprobada ni rechazada.</summary>
    public bool EstaAbierta => Estado is EstadoEtapa.Pendiente or EstadoEtapa.EnRevision or EstadoEtapa.Observado;

    /// <summary>Solo se pueden cargar documentos mientras la etapa está Pendiente u Observada.</summary>
    public bool AdmiteDocumentos => Estado is EstadoEtapa.Pendiente or EstadoEtapa.Observado;

    internal static Etapa Abrir(Guid tesisId, EtapaTipo tipo, DateTime utcNow) => new()
    {
        TesisId = tesisId,
        Tipo = Enum.IsDefined(tipo) ? tipo : throw new DomainException("El tipo de etapa no es válido."),
        Estado = EstadoEtapa.Pendiente,
        FechaApertura = utcNow,
    };

    /// <summary>Pendiente u Observado → EnRevision.</summary>
    internal void EnviarARevision(DateTime utcNow)
    {
        if (Estado is not (EstadoEtapa.Pendiente or EstadoEtapa.Observado))
        {
            throw new TransicionInvalidaException(Estado == EstadoEtapa.EnRevision
                ? "La etapa ya está en revisión."
                : $"La etapa está {Estado} y no admite nuevos envíos.");
        }

        Estado = EstadoEtapa.EnRevision;
        EnRevisionDesde = utcNow;
        Revision++;
    }

    /// <summary>EnRevision → Aprobado, Observado o Rechazado, según la decisión del Administrador.</summary>
    internal void Resolver(ResultadoDecision resultado, DateTime utcNow)
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

        if (Estado is EstadoEtapa.Aprobado or EstadoEtapa.Rechazado)
        {
            FechaCierre = utcNow;
        }

        Revision++;
    }
}
