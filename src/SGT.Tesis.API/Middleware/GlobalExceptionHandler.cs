using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SGT.Tesis.Application.Common.Exceptions;
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
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var problem = Mapear(exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Excepción no controlada en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("{Titulo} en {Method} {Path}", problem.Title, httpContext.Request.Method, httpContext.Request.Path);
        }

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;

        if (problem.Status == StatusCodes.Status401Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
        }

        // Se serializa con el tipo concreto para no perder los errores por campo de ValidationProblemDetails.
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }

    private static ProblemDetails Mapear(Exception exception) => exception switch
    {
        ValidacionException v => new ValidationProblemDetails(v.Errores)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o más campos son inválidos.",
        },
        NoAutenticadoException e => Crear(StatusCodes.Status401Unauthorized, "No autenticado.", e.Message),
        ProhibidoException e => Crear(StatusCodes.Status403Forbidden, "Acceso denegado.", e.Message),
        NoEncontradoException e => Crear(StatusCodes.Status404NotFound, "Recurso no encontrado.", e.Message),
        ConflictoException e => Crear(StatusCodes.Status409Conflict, "Conflicto con el estado actual.", e.Message),
        DomainException e => Crear(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida.", e.Message),
        _ => Crear(
            StatusCodes.Status500InternalServerError,
            "Ocurrió un error inesperado.",
            "Si el problema persiste, comuníquelo indicando el identificador de la solicitud."),
    };

    private static ProblemDetails Crear(int status, string titulo, string detalle) => new()
    {
        Status = status,
        Title = titulo,
        Detail = detalle,
    };
}
