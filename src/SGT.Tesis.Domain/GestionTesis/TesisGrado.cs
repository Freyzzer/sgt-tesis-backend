using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;

namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>Raíz de agregado del proceso de tesis de grado de un estudiante.</summary>
public sealed class TesisGrado : BaseEntity
{
    public const int TituloMaxLength = 300;
    public const int ResumenMaxLength = 3000;

    private readonly List<Etapa> _etapas = [];

    private TesisGrado()
    {
        Titulo = string.Empty;
        Resumen = string.Empty;
    }

    public Guid StudentId { get; private set; }

    public string Titulo { get; private set; }

    public string Resumen { get; private set; }

    public EstadoTesis Estado { get; private set; }

    public IReadOnlyCollection<Etapa> Etapas => _etapas;

    /// <summary>La etapa más reciente: la única que puede estar abierta (INV-1).</summary>
    public Etapa EtapaActual => _etapas.OrderByDescending(e => e.Tipo).First();

    /// <summary>Crea la tesis con la primera etapa (Estudiante) abierta en Pendiente.</summary>
    public static TesisGrado Crear(Guid studentId, string? titulo, string? resumen, DateTime utcNow)
    {
        var tesis = new TesisGrado
        {
            StudentId = Guard.NotEmpty(studentId, nameof(StudentId)),
            Titulo = Guard.Required(titulo, TituloMaxLength, nameof(Titulo)),
            Resumen = Guard.Required(resumen, ResumenMaxLength, nameof(Resumen)),
            Estado = EstadoTesis.EnCurso,
        };

        tesis._etapas.Add(Etapa.Abrir(tesis.Id, EtapaTipo.Estudiante, utcNow));

        return tesis;
    }

    public Etapa ObtenerEtapa(EtapaTipo tipo) =>
        _etapas.FirstOrDefault(e => e.Tipo == tipo)
        ?? throw new TransicionInvalidaException($"La etapa {tipo} aún no está abierta para esta tesis.");

    /// <summary>Valida que la etapa exista y admita nuevos documentos (INV-7).</summary>
    public Etapa ExigirEtapaEditable(EtapaTipo tipo)
    {
        var etapa = ObtenerEtapa(tipo);

        if (!etapa.AdmiteDocumentos)
        {
            throw new TransicionInvalidaException(etapa.Estado switch
            {
                EstadoEtapa.EnRevision => "La etapa está en revisión y no admite cambios de documentos.",
                _ => $"La etapa está {etapa.Estado} y no admite documentos.",
            });
        }

        return etapa;
    }

    /// <summary>Envía a revisión la etapa indicada, que debe ser la actual y estar Pendiente u Observada.</summary>
    public Etapa EnviarEtapaARevision(EtapaTipo tipo, DateTime utcNow)
    {
        if (Estado != EstadoTesis.EnCurso)
        {
            throw new TransicionInvalidaException("La tesis ya no está en curso.");
        }

        var etapa = ObtenerEtapa(tipo);

        if (etapa.Tipo != EtapaActual.Tipo)
        {
            throw new TransicionInvalidaException("La etapa indicada no es la etapa actual de la tesis.");
        }

        etapa.EnviarARevision(utcNow);

        return etapa;
    }

    /// <summary>
    /// Registra la decisión del Administrador sobre la etapa actual, que debe estar en revisión.
    /// Aprobar abre la etapa siguiente (o finaliza la tesis tras la 7); rechazar rechaza la tesis; observar devuelve la etapa al estudiante.
    /// </summary>
    public Decision Decidir(EtapaTipo tipo, ResultadoDecision resultado, Guid administradorId, string? justificacion, DateTime utcNow)
    {
        if (Estado != EstadoTesis.EnCurso)
        {
            throw new TransicionInvalidaException("La tesis ya no está en curso.");
        }

        var etapa = ObtenerEtapa(tipo);

        if (etapa.Tipo != EtapaActual.Tipo)
        {
            throw new TransicionInvalidaException("La etapa indicada no es la etapa actual de la tesis.");
        }

        // INV-2: no se aprueba la etapa N si la N-1 no está aprobada.
        if (resultado == ResultadoDecision.Aprobada && tipo > EtapaTipo.Estudiante
            && ObtenerEtapa(tipo - 1).Estado != EstadoEtapa.Aprobado)
        {
            throw new TransicionInvalidaException("La etapa anterior debe estar aprobada.");
        }

        var decision = Decision.Crear(etapa.Id, administradorId, resultado, justificacion, utcNow);
        etapa.Resolver(resultado, utcNow);

        switch (resultado)
        {
            case ResultadoDecision.Aprobada when tipo == EtapaTipo.RequisitosFinales:
                Estado = EstadoTesis.Finalizada;
                break;
            case ResultadoDecision.Aprobada:
                _etapas.Add(Etapa.Abrir(Id, tipo + 1, utcNow));
                break;
            case ResultadoDecision.Rechazada:
                Estado = EstadoTesis.Rechazada;
                break;
        }

        return decision;
    }
}
