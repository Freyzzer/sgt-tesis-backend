using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;

namespace SGT.Tesis.Application.GestionTesis;

public sealed record ObtenerMiTesisQuery;

public sealed class ObtenerMiTesisHandler(ITesisRepository tesis, IDecisionRepository decisiones, ICurrentUser actual)
    : IQueryHandler<ObtenerMiTesisQuery, TesisDto>
{
    public async Task<TesisDto> HandleAsync(ObtenerMiTesisQuery query, CancellationToken cancellationToken)
    {
        var studentId = AutorizacionTesis.ExigirEstudiante(actual);

        var propia = await tesis.ObtenerActivaDeEstudianteAsync(studentId, cancellationToken)
            ?? throw new NoEncontradoException("Aún no ha registrado una tesis.");

        return TesisDto.Desde(propia, await decisiones.ListarPorTesisAsync(propia.Id, cancellationToken));
    }
}

public sealed record ObtenerTesisQuery(Guid Id);

public sealed class ObtenerTesisHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IAsignacionRepository asignaciones,
    ICurrentUser actual)
    : IQueryHandler<ObtenerTesisQuery, TesisDto>
{
    public async Task<TesisDto> HandleAsync(ObtenerTesisQuery query, CancellationToken cancellationToken)
    {
        var encontrada = await tesis.ObtenerPorIdAsync(query.Id, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        await AutorizacionTesis.ExigirVisibleAsync(encontrada, actual, asignaciones, cancellationToken);

        return TesisDto.Desde(encontrada, await decisiones.ListarPorTesisAsync(encontrada.Id, cancellationToken));
    }
}
