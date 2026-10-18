using SGT.Tesis.Domain.Common;

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
    internal void EnviarARevision()
    {
        if (Estado is not (EstadoEtapa.Pendiente or EstadoEtapa.Observado))
        {
            throw new TransicionInvalidaException(Estado == EstadoEtapa.EnRevision
                ? "La etapa ya está en revisión."
                : $"La etapa está {Estado} y no admite nuevos envíos.");
        }

        Estado = EstadoEtapa.EnRevision;
    }
}
