using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Evaluacion;

/// <summary>Asignación de un Evaluador a una etapa concreta; solo mientras está activa puede emitir veredictos.</summary>
public sealed class AsignacionEvaluador : BaseEntity
{
    private AsignacionEvaluador()
    {
    }

    public Guid EtapaId { get; private set; }

    public Guid EvaluadorId { get; private set; }

    public Guid AsignadoPorId { get; private set; }

    public bool Activa { get; private set; }

    public DateTime FechaAsignacion { get; private set; }

    public DateTime? FechaRetiro { get; private set; }

    public static AsignacionEvaluador Crear(Guid etapaId, Guid evaluadorId, Guid asignadoPorId, DateTime utcNow) => new()
    {
        EtapaId = Guard.NotEmpty(etapaId, nameof(EtapaId)),
        EvaluadorId = Guard.NotEmpty(evaluadorId, nameof(EvaluadorId)),
        AsignadoPorId = Guard.NotEmpty(asignadoPorId, nameof(AsignadoPorId)),
        Activa = true,
        FechaAsignacion = utcNow,
    };

    public void Retirar(DateTime utcNow)
    {
        if (!Activa)
        {
            return;
        }

        Activa = false;
        FechaRetiro = utcNow;
    }
}

public enum ConceptoVeredicto
{
    Favorable = 1,
    ConObservaciones = 2,
    Desfavorable = 3,
}

/// <summary>
/// Concepto técnico no vinculante de un Evaluador. Cada actualización crea una versión nueva y las
/// anteriores se conservan (historial); la vigente es la de mayor versión.
/// </summary>
public sealed class Veredicto : BaseEntity
{
    public const int ComentariosMaxLength = 4000;

    private Veredicto()
    {
        Comentarios = string.Empty;
    }

    public Guid EtapaId { get; private set; }

    public Guid EvaluadorId { get; private set; }

    public ConceptoVeredicto Concepto { get; private set; }

    public string Comentarios { get; private set; }

    public int Version { get; private set; }

    /// <summary>Ronda de la etapa en la que se emitió; solo los veredictos de la ronda actual cuentan para aprobar.</summary>
    public int Ronda { get; private set; }

    public static Veredicto Crear(Guid etapaId, Guid evaluadorId, ConceptoVeredicto concepto, string? comentarios, int version, int ronda = 1)
    {
        if (!Enum.IsDefined(concepto))
        {
            throw new DomainException("El concepto del veredicto no es válido.");
        }

        if (version < 1)
        {
            throw new DomainException("La versión debe ser mayor o igual a 1.");
        }

        if (ronda < 1)
        {
            throw new DomainException("La ronda debe ser mayor o igual a 1.");
        }

        return new Veredicto
        {
            EtapaId = Guard.NotEmpty(etapaId, nameof(EtapaId)),
            EvaluadorId = Guard.NotEmpty(evaluadorId, nameof(EvaluadorId)),
            Concepto = concepto,
            Comentarios = Guard.Required(comentarios, ComentariosMaxLength, nameof(Comentarios)),
            Version = version,
            Ronda = ronda,
        };
    }
}
