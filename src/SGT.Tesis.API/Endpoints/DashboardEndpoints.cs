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

        // Descargas CSV (FR-16). El separador por defecto es la coma; Excel en español prefiere ?separador=PuntoYComa.
        group.MapGet("/kpis/exportar", async (
                [AsParameters] ExportarKpisCsvQuery query,
                IQueryHandler<ExportarKpisCsvQuery, ArchivoCsv> handler,
                CancellationToken ct) =>
            {
                var archivo = await handler.HandleAsync(query, ct);
                return Results.File(archivo.Contenido, "text/csv; charset=utf-8", archivo.NombreArchivo);
            })
            .AddEndpointFilter<ValidationFilter<ExportarKpisCsvQuery>>()
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        group.MapGet("/cola-validacion/exportar", async (
                [AsParameters] ExportarColaCsvQuery query,
                IQueryHandler<ExportarColaCsvQuery, ArchivoCsv> handler,
                CancellationToken ct) =>
            {
                var archivo = await handler.HandleAsync(query, ct);
                return Results.File(archivo.Contenido, "text/csv; charset=utf-8", archivo.NombreArchivo);
            })
            .AddEndpointFilter<ValidationFilter<ExportarColaCsvQuery>>()
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        return routes;
    }
}
