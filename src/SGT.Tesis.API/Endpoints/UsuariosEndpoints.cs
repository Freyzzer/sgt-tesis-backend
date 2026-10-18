using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.API.Security;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.API.Endpoints;

public sealed record CambiarEstadoRequest(bool Activo);

public sealed record CambiarRolRequest(Rol Rol);

public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/usuarios")
            .WithTags("Usuarios")
            .RequireAuthorization(Politicas.Administrador);

        group.MapPost("/", async (
                CrearUsuarioCommand command,
                ICommandHandler<CrearUsuarioCommand, UsuarioDto> handler,
                CancellationToken ct) =>
            {
                var usuario = await handler.HandleAsync(command, ct);
                return Results.Created($"/api/v1/usuarios/{usuario.Id}", usuario);
            })
            .AddEndpointFilter<ValidationFilter<CrearUsuarioCommand>>()
            .Produces<UsuarioDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (
                [AsParameters] ListarUsuariosQuery query,
                IQueryHandler<ListarUsuariosQuery, PagedResult<UsuarioDto>> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(query, ct)))
            .AddEndpointFilter<ValidationFilter<ListarUsuariosQuery>>()
            .Produces<PagedResult<UsuarioDto>>();

        group.MapGet("/{id:guid}", async (
                Guid id,
                IQueryHandler<ObtenerUsuarioQuery, UsuarioDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ObtenerUsuarioQuery(id), ct)))
            .Produces<UsuarioDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/estado", async (
                Guid id,
                CambiarEstadoRequest body,
                ICommandHandler<CambiarEstadoUsuarioCommand, UsuarioDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new CambiarEstadoUsuarioCommand(id, body.Activo), ct)))
            .Produces<UsuarioDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/rol", async (
                Guid id,
                CambiarRolRequest body,
                ICommandHandler<CambiarRolUsuarioCommand, UsuarioDto> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(new CambiarRolUsuarioCommand(id, body.Rol), ct)))
            .Produces<UsuarioDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }
}
