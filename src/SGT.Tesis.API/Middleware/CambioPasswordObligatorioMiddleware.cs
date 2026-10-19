using Microsoft.AspNetCore.Mvc;

namespace SGT.Tesis.API.Middleware;

/// <summary>
/// Mientras la cuenta tenga una contraseña temporal (<c>DebeCambiarPassword</c>), solo se permite cambiarla o cerrar
/// la sesión; el resto de la API responde 403 con el código <c>cambio_password_obligatorio</c>.
/// </summary>
public sealed class CambioPasswordObligatorioMiddleware(RequestDelegate next)
{
    public const string ClaimPendiente = "cambio_password_pendiente";

    private static readonly string[] RutasPermitidas =
    [
        "/api/v1/auth/cambiar-password",
        "/api/v1/auth/logout",
        "/health",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var pendiente = context.User.Identity?.IsAuthenticated == true && context.User.HasClaim(c => c.Type == ClaimPendiente);

        if (pendiente && !RutasPermitidas.Any(r => context.Request.Path.StartsWithSegments(r, StringComparison.OrdinalIgnoreCase)))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Debe cambiar su contraseña.",
                Detail = "Su contraseña es temporal. Cámbiela en /api/v1/auth/cambiar-password para continuar.",
            };
            problem.Extensions["codigo"] = "cambio_password_obligatorio";

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}
