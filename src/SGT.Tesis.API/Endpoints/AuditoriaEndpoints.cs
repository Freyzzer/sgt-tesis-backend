using SGT.Tesis.API.Security;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Usuarios;

namespace SGT.Tesis.API.Endpoints;

public static class AuditoriaEndpoints
{
    public static IEndpointRouteBuilder MapAuditoriaEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/auditoria", async (
                [AsParameters] ConsultarAuditoriaQuery query,
                IQueryHandler<ConsultarAuditoriaQuery, PagedResult<RegistroAuditoriaDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(query, ct)))
            .WithTags("Auditoría")
            .RequireAuthorization(Politicas.Administrador)
            .AddEndpointFilter<ValidationFilter<ConsultarAuditoriaQuery>>()
            .Produces<PagedResult<RegistroAuditoriaDto>>();

        return routes;
    }
}
