using SGT.Tesis.Domain.Common;

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

    /// <summary>Envía a revisión la etapa indicada, que debe ser la actual y estar Pendiente u Observada.</summary>
    public Etapa EnviarEtapaARevision(EtapaTipo tipo)
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

        etapa.EnviarARevision();

        return etapa;
    }
}
