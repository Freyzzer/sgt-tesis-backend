using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Consultas;
using SGT.Tesis.Application.Usuarios;

namespace SGT.Tesis.API.Endpoints;

public static class ConsultasEndpoints
{
    public static IEndpointRouteBuilder MapConsultasEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/evaluaciones")
            .WithTags("Evaluaciones")
            .RequireAuthorization();

        group.MapGet("/mias", async (
                [AsParameters] ListarMisEvaluacionesQuery query,
                IQueryHandler<ListarMisEvaluacionesQuery, PagedResult<EvaluacionAsignadaDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(query, ct)))
            .AddEndpointFilter<ValidationFilter<ListarMisEvaluacionesQuery>>()
            .Produces<PagedResult<EvaluacionAsignadaDto>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return routes;
    }
}
