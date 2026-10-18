using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.API.Middleware;

/// <summary>
/// Convierte excepciones no controladas en <c>ProblemDetails</c> (RFC 9457) sin exponer trazas
/// ni detalles internos al cliente.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Regla de negocio incumplida.",
                Detail = domain.Message,
            },
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => null,
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado.",
                Detail = "Si el problema persiste, comuníquelo indicando el identificador de la solicitud.",
            },
        };

        if (problem is null)
        {
            return true;
        }

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Excepción no controlada en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Regla de negocio incumplida en {Method} {Path}: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
