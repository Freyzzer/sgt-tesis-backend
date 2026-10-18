using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.API.Security;
using SGT.Tesis.Application.Common.Abstractions;
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
