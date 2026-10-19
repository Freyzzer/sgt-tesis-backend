using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Decisiones;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;

namespace SGT.Tesis.API.Endpoints;

public sealed record AsignarEvaluadorRequest(Guid EvaluadorId);

public sealed record EmitirVeredictoRequest(ConceptoVeredicto Concepto, string Comentarios);

public sealed record DecidirEtapaRequest(ResultadoDecision Resultado, string? Justificacion);

public static class EvaluacionEndpoints
{
    public static IEndpointRouteBuilder MapEvaluacionEndpoints(this IEndpointRouteBuilder routes)
    {
        var etapa = routes.MapGroup("/api/v1/tesis/{id:guid}/etapas/{numero:int}")
            .WithTags("Evaluación y decisiones")
            .RequireAuthorization();

        etapa.MapPost("/evaluadores", async (
                Guid id,
                int numero,
                AsignarEvaluadorRequest body,
                ICommandHandler<AsignarEvaluadorCommand, AsignacionDto> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                var command = new AsignarEvaluadorCommand(id, numero, body.EvaluadorId);
                await ValidationFilter<AsignarEvaluadorCommand>.ValidarAsync(command, services, ct);
                var asignacion = await handler.HandleAsync(command, ct);

                return Results.Created($"/api/v1/tesis/{id}/etapas/{numero}/evaluadores", asignacion);
            })
            .Produces<AsignacionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        etapa.MapGet("/evaluadores", async (
                Guid id,
                int numero,
                IQueryHandler<ListarAsignacionesQuery, IReadOnlyList<AsignacionDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ListarAsignacionesQuery(id, numero), ct)))
            .Produces<IReadOnlyList<AsignacionDto>>();

        etapa.MapDelete("/evaluadores/{evaluadorId:guid}", async (
                Guid id,
                int numero,
                Guid evaluadorId,
                ICommandHandler<RetirarEvaluadorCommand, bool> handler,
                CancellationToken ct) =>
            {
                await handler.HandleAsync(new RetirarEvaluadorCommand(id, numero, evaluadorId), ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        etapa.MapPut("/veredicto", async (
                Guid id,
                int numero,
                EmitirVeredictoRequest body,
                ICommandHandler<EmitirVeredictoCommand, VeredictoDto> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                var command = new EmitirVeredictoCommand(id, numero, body.Concepto, body.Comentarios ?? string.Empty);
                await ValidationFilter<EmitirVeredictoCommand>.ValidarAsync(command, services, ct);

                return Results.Ok(await handler.HandleAsync(command, ct));
            })
            .Produces<VeredictoDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        etapa.MapGet("/veredictos", async (
                Guid id,
                int numero,
                bool? soloVigentes,
                IQueryHandler<ListarVeredictosQuery, IReadOnlyList<VeredictoDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ListarVeredictosQuery(id, numero, soloVigentes ?? false), ct)))
            .Produces<IReadOnlyList<VeredictoDto>>();

        etapa.MapPost("/decisiones", async (
                Guid id,
                int numero,
                DecidirEtapaRequest body,
                ICommandHandler<DecidirEtapaCommand, TesisDto> handler,
                IServiceProvider services,
                CancellationToken ct) =>
            {
                var command = new DecidirEtapaCommand(id, numero, body.Resultado, body.Justificacion);
                await ValidationFilter<DecidirEtapaCommand>.ValidarAsync(command, services, ct);
                var tesis = await handler.HandleAsync(command, ct);

                return Results.Created($"/api/v1/tesis/{id}", tesis);
            })
            .Produces<TesisDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }
}
