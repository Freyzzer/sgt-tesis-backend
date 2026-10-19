using SGT.Tesis.API.Security;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Reportes;
using SGT.Tesis.Application.Usuarios;

namespace SGT.Tesis.API.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization(Politicas.Administrador);

        group.MapGet("/kpis", async (
                [AsParameters] ObtenerKpisQuery query,
                IQueryHandler<ObtenerKpisQuery, KpisDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(query, ct)))
            .AddEndpointFilter<ValidationFilter<ObtenerKpisQuery>>()
            .Produces<KpisDto>();

        group.MapGet("/cola-validacion", async (
                [AsParameters] ListarColaValidacionQuery query,
                IQueryHandler<ListarColaValidacionQuery, PagedResult<ItemColaValidacionDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(query, ct)))
            .AddEndpointFilter<ValidationFilter<ListarColaValidacionQuery>>()
            .Produces<PagedResult<ItemColaValidacionDto>>();

        return routes;
    }
}
