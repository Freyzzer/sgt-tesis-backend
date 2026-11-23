using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Documentos;

public sealed record ListarDocumentosQuery(Guid TesisId, int Etapa);

public sealed class ListarDocumentosQueryValidator : AbstractValidator<ListarDocumentosQuery>
{
    public ListarDocumentosQueryValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
    }
}

public sealed class ListarDocumentosHandler(
    ITesisRepository tesis,
    IDocumentoRepository documentos,
    IAsignacionRepository asignaciones,
    ICurrentUser actual)
    : IQueryHandler<ListarDocumentosQuery, IReadOnlyList<DocumentoDto>>
{
    public async Task<IReadOnlyList<DocumentoDto>> HandleAsync(ListarDocumentosQuery query, CancellationToken cancellationToken)
    {
        var encontrada = await tesis.ObtenerPorIdAsync(query.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        await AutorizacionTesis.ExigirVisibleAsync(encontrada, actual, asignaciones, cancellationToken);

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)query.Etapa);
        var lista = await documentos.ListarPorEtapaAsync(etapa.Id, cancellationToken);

        return lista.Select(DocumentoDto.Desde).ToList();
    }
}

public sealed record DescargarDocumentoQuery(Guid DocumentoId);

public sealed record ArchivoDescarga(Stream Contenido, string NombreOriginal, string ContentType, long TamanoBytes);

public sealed class DescargarDocumentoHandler(
    IDocumentoRepository documentos,
    ITesisRepository tesis,
    IAsignacionRepository asignaciones,
    IFileStorage storage,
    ICurrentUser actual) : IQueryHandler<DescargarDocumentoQuery, ArchivoDescarga>
{
    public async Task<ArchivoDescarga> HandleAsync(DescargarDocumentoQuery query, CancellationToken cancellationToken)
    {
        var documento = await documentos.ObtenerPorIdAsync(query.DocumentoId, cancellationToken)
            ?? throw new NoEncontradoException("El documento no existe.");

        var tesisDelDocumento = await tesis.ObtenerPorEtapaAsync(documento.EtapaId, cancellationToken)
            ?? throw new NoEncontradoException("El documento no existe.");

        // Dueño, Administrador y Evaluadores con asignación activa en la tesis.
        await AutorizacionTesis.ExigirVisibleAsync(tesisDelDocumento, actual, asignaciones, cancellationToken);

        var contenido = await storage.AbrirLecturaAsync(documento.NombreAlmacenado, cancellationToken);

        return new ArchivoDescarga(contenido, documento.NombreOriginal, documento.ContentType, documento.TamanoBytes);
    }
}
