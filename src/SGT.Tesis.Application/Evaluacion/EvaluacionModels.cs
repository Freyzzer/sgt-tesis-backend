using SGT.Tesis.Domain.Evaluacion;

namespace SGT.Tesis.Application.Evaluacion;

public sealed record AsignacionDto(Guid Id, Guid EtapaId, Guid EvaluadorId, bool Activa, DateTime FechaAsignacion, DateTime? FechaRetiro)
{
    public static AsignacionDto Desde(AsignacionEvaluador a) => new(a.Id, a.EtapaId, a.EvaluadorId, a.Activa, a.FechaAsignacion, a.FechaRetiro);
}

public sealed record VeredictoDto(Guid Id, Guid EtapaId, Guid EvaluadorId, string Concepto, string Comentarios, int Version, DateTime CreatedAt)
{
    public static VeredictoDto Desde(Veredicto v) => new(v.Id, v.EtapaId, v.EvaluadorId, v.Concepto.ToString(), v.Comentarios, v.Version, v.CreatedAt);
}

public interface IAsignacionRepository
{
    Task<AsignacionEvaluador?> ObtenerActivaAsync(Guid etapaId, Guid evaluadorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AsignacionEvaluador>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken);

    /// <summary>Indica si el evaluador tiene una asignación activa en alguna etapa de la tesis.</summary>
    Task<bool> TieneAsignacionActivaEnTesisAsync(Guid evaluadorId, Guid tesisId, CancellationToken cancellationToken);

    void Agregar(AsignacionEvaluador asignacion);
}

public interface IVeredictoRepository
{
    Task<int> UltimaVersionAsync(Guid etapaId, Guid evaluadorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Veredicto>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken);

    void Agregar(Veredicto veredicto);
}
