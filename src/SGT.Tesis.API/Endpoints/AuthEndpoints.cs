using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.API.Security;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Contrasenas;
using SGT.Tesis.Application.Identidad.Dtos;
using SGT.Tesis.Application.Identidad.Login;
using SGT.Tesis.Application.Identidad.Logout;
using SGT.Tesis.Application.Identidad.Refresh;

namespace SGT.Tesis.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth").WithTags("Autenticación");

        group.MapPost("/login", async (
                LoginCommand command,
                ICommandHandler<LoginCommand, TokenResponse> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(command, ct)))
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .RequireRateLimiting(Politicas.LimiteLogin)
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
                RefreshCommand command,
                ICommandHandler<RefreshCommand, TokenResponse> handler,
                CancellationToken ct) => Results.Ok(await handler.HandleAsync(command, ct)))
            .AddEndpointFilter<ValidationFilter<RefreshCommand>>()
            .RequireRateLimiting(Politicas.LimiteLogin)
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/cambiar-password", async (
                CambiarPasswordCommand command,
                ICommandHandler<CambiarPasswordCommand, bool> handler,
                CancellationToken ct) =>
            {
                await handler.HandleAsync(command, ct);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<CambiarPasswordCommand>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Responde siempre 202: no revela si el correo está registrado.
        group.MapPost("/olvide-password", async (
                OlvidePasswordCommand command,
                ICommandHandler<OlvidePasswordCommand, bool> handler,
                CancellationToken ct) =>
            {
                await handler.HandleAsync(command, ct);
                return Results.Accepted();
            })
            .AddEndpointFilter<ValidationFilter<OlvidePasswordCommand>>()
            .RequireRateLimiting(Politicas.LimiteLogin)
            .Produces(StatusCodes.Status202Accepted);

        group.MapPost("/restablecer-password", async (
                RestablecerPasswordCommand command,
                ICommandHandler<RestablecerPasswordCommand, bool> handler,
                CancellationToken ct) =>
            {
                await handler.HandleAsync(command, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<RestablecerPasswordCommand>>()
            .RequireRateLimiting(Politicas.LimiteLogin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/logout", async (
                LogoutCommand command,
                ICommandHandler<LogoutCommand, bool> handler,
                CancellationToken ct) =>
            {
                await handler.HandleAsync(command, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<LogoutCommand>>()
            .Produces(StatusCodes.Status204NoContent);

        return routes;
    }
}
