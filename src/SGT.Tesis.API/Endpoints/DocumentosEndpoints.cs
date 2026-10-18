using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Documentos;

namespace SGT.Tesis.API.Endpoints;

public static class DocumentosEndpoints
{
    public static IEndpointRouteBuilder MapDocumentosEndpoints(this IEndpointRouteBuilder routes)
    {
        var etapa = routes.MapGroup("/api/v1/tesis/{id:guid}/etapas/{numero:int}/documentos")
            .WithTags("Documentos")
            .RequireAuthorization();

        etapa.MapPost("/", async (
                Guid id,
                int numero,
                [FromForm] string? categoria,
                IFormFile? archivo,
                ICommandHandler<SubirDocumentoCommand, DocumentoDto> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                if (archivo is null)
                {
                    throw new ValidacionException(new Dictionary<string, string[]> { ["archivo"] = ["El archivo es obligatorio."] });
                }

                await using var contenido = archivo.OpenReadStream();
                var command = new SubirDocumentoCommand(id, numero, categoria ?? string.Empty, archivo.FileName, contenido);
                await ValidationFilter<SubirDocumentoCommand>.ValidarAsync(command, services, ct);

                var documento = await handler.HandleAsync(command, ct);

                return Results.Created($"/api/v1/documentos/{documento.Id}", documento);
            })
            .DisableAntiforgery() // autenticación por token Bearer, no por cookie
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        etapa.MapGet("/", async (
                Guid id,
                int numero,
                IQueryHandler<ListarDocumentosQuery, IReadOnlyList<DocumentoDto>> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                var query = new ListarDocumentosQuery(id, numero);
                await ValidationFilter<ListarDocumentosQuery>.ValidarAsync(query, services, ct);

                return Results.Ok(await handler.HandleAsync(query, ct));
            })
            .Produces<IReadOnlyList<DocumentoDto>>();

        routes.MapGet("/api/v1/documentos/{id:guid}/contenido", async (
                Guid id,
                IQueryHandler<DescargarDocumentoQuery, ArchivoDescarga> handler,
                CancellationToken ct) =>
            {
                var archivo = await handler.HandleAsync(new DescargarDocumentoQuery(id), ct);

                // fileDownloadName fuerza Content-Disposition: attachment.
                return Results.File(archivo.Contenido, archivo.ContentType, archivo.NombreOriginal);
            })
            .WithTags("Documentos")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }
}
