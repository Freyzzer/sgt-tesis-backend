using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Reportes;

public sealed record TesisPorEtapaDto(int Numero, string Tipo, int Cantidad);

public sealed record TiempoPorEtapaDto(int Numero, string Tipo, int EtapasCerradas, double PromedioDias);

public sealed record DecisionesKpiDto(int Total, int Aprobadas, int Observadas, int Rechazadas, double TasaAprobacion, double TasaObservacion, double TasaRechazo);

public sealed record CargaEvaluadorDto(Guid EvaluadorId, string Nombre, int AsignacionesActivas, int VeredictosPendientes);

public sealed record KpisDto(
    DateTime GeneradoEn,
    int TotalTesis,
    IReadOnlyDictionary<string, int> TesisPorEstado,
    IReadOnlyList<TesisPorEtapaDto> TesisEnCursoPorEtapa,
    IReadOnlyList<TiempoPorEtapaDto> TiempoPromedioPorEtapa,
    DecisionesKpiDto Decisiones,
    int EtapasEnRevision,
    int DiasEstancamiento,
    int EtapasEstancadas,
    IReadOnlyList<CargaEvaluadorDto> CargaPorEvaluador);

public sealed record ItemColaValidacionDto(
    Guid TesisId,
    Guid EstudianteId,
    string Estudiante,
    string Titulo,
    int Etapa,
    string TipoEtapa,
    DateTime EnRevisionDesde,
    int DiasEnRevision,
    int EvaluadoresAsignados,
    int EvaluadoresConVeredicto);

public interface IReportesRepository
{
    Task<KpisDto> ObtenerKpisAsync(DateTime ahora, int diasEstancamiento, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ItemColaValidacionDto> Items, int Total)> ListarColaAsync(
        DateTime ahora,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record ObtenerKpisQuery(int DiasEstancamiento = 15);

public sealed class ObtenerKpisQueryValidator : AbstractValidator<ObtenerKpisQuery>
{
    public ObtenerKpisQueryValidator()
    {
        RuleFor(x => x.DiasEstancamiento).InclusiveBetween(1, 365);
    }
}

public sealed class ObtenerKpisHandler(IReportesRepository reportes, ICurrentUser actual, IClock clock)
    : IQueryHandler<ObtenerKpisQuery, KpisDto>
{
    public Task<KpisDto> HandleAsync(ObtenerKpisQuery query, CancellationToken cancellationToken)
    {
        AdministradorRequerido.Exigir(actual);

        return reportes.ObtenerKpisAsync(clock.UtcNow, query.DiasEstancamiento, cancellationToken);
    }
}

public sealed record ListarColaValidacionQuery(int Page = 1, int PageSize = 20);

public sealed class ListarColaValidacionQueryValidator : AbstractValidator<ListarColaValidacionQuery>
{
    public ListarColaValidacionQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListarUsuariosQueryValidator.PageSizeMaximo);
    }
}

/// <summary>Etapas en revisión, de la que más tiempo lleva esperando a la más reciente.</summary>
public sealed class ListarColaValidacionHandler(IReportesRepository reportes, ICurrentUser actual, IClock clock)
    : IQueryHandler<ListarColaValidacionQuery, PagedResult<ItemColaValidacionDto>>
{
    public async Task<PagedResult<ItemColaValidacionDto>> HandleAsync(ListarColaValidacionQuery query, CancellationToken cancellationToken)
    {
        AdministradorRequerido.Exigir(actual);

        var (items, total) = await reportes.ListarColaAsync(clock.UtcNow, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<ItemColaValidacionDto>(items, query.Page, query.PageSize, total);
    }
}

internal static class AdministradorRequerido
{
    public static void Exigir(ICurrentUser actual)
    {
        if (actual.UserId is null || actual.Rol != nameof(Rol.Administrador))
        {
            throw new ProhibidoException();
        }
    }
}
