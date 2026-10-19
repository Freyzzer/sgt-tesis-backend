using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Application.Retroalimentacion;

namespace SGT.Tesis.API.Endpoints;

public static class TesisEndpoints
{
    public static IEndpointRouteBuilder MapTesisEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/tesis")
            .WithTags("Tesis")
            .RequireAuthorization();

        group.MapPost("/", async (
                RegistrarTesisCommand command,
                ICommandHandler<RegistrarTesisCommand, TesisDto> handler,
                CancellationToken ct) =>
            {
                var tesis = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/v1/tesis/{tesis.Id}", tesis);
            })
            .AddEndpointFilter<ValidationFilter<RegistrarTesisCommand>>()
            .Produces<TesisDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/mia", async (
                IQueryHandler<ObtenerMiTesisQuery, TesisDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ObtenerMiTesisQuery(), ct)))
            .Produces<TesisDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerTesisQuery, TesisDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ObtenerTesisQuery(id), ct)))
            .Produces<TesisDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/retroalimentacion", async (
                Guid id,
                IQueryHandler<ObtenerRetroalimentacionQuery, RetroalimentacionDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ObtenerRetroalimentacionQuery(id), ct)))
            .Produces<RetroalimentacionDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/etapas/{numero:int}/enviar", async (
                Guid id,
                int numero,
                ICommandHandler<EnviarEtapaARevisionCommand, TesisDto> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                var command = new EnviarEtapaARevisionCommand(id, numero);
                await ValidationFilter<EnviarEtapaARevisionCommand>.ValidarAsync(command, services, ct);

                return Results.Ok(await handler.HandleAsync(command, ct));
            })
            .Produces<TesisDto>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return routes;
    }
}
