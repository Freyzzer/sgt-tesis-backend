using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;

namespace SGT.Tesis.Application.GestionTesis;

public sealed record ObtenerMiTesisQuery;

public sealed class ObtenerMiTesisHandler(ITesisRepository tesis, ICurrentUser actual)
    : IQueryHandler<ObtenerMiTesisQuery, TesisDto>
{
    public async Task<TesisDto> HandleAsync(ObtenerMiTesisQuery query, CancellationToken cancellationToken)
    {
        var studentId = AutorizacionTesis.ExigirEstudiante(actual);

        var propia = await tesis.ObtenerActivaDeEstudianteAsync(studentId, cancellationToken)
            ?? throw new NoEncontradoException("Aún no ha registrado una tesis.");

        return TesisDto.Desde(propia);
    }
}

public sealed record ObtenerTesisQuery(Guid Id);

public sealed class ObtenerTesisHandler(ITesisRepository tesis, ICurrentUser actual)
    : IQueryHandler<ObtenerTesisQuery, TesisDto>
{
    public async Task<TesisDto> HandleAsync(ObtenerTesisQuery query, CancellationToken cancellationToken)
    {
        var encontrada = await tesis.ObtenerPorIdAsync(query.Id, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        AutorizacionTesis.ExigirVisible(encontrada, actual);

        return TesisDto.Desde(encontrada);
    }
}
