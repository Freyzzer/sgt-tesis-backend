using System.Text.RegularExpressions;
using Serilog.Context;
using SGT.Tesis.Application.Auditoria;

namespace SGT.Tesis.API.Middleware;

/// <summary>
/// Asigna a cada solicitud un identificador de correlación: usa el de la cabecera
/// <c>X-Correlation-Id</c> si es válido (para seguir una operación entre sistemas) o genera uno nuevo.
/// Se devuelve en la respuesta, se agrega a los logs y se guarda en la auditoría.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex Valido();

    public async Task InvokeAsync(HttpContext context)
    {
        var recibido = context.Request.Headers[Header].FirstOrDefault();
        var id = recibido is not null && Valido().IsMatch(recibido) ? recibido : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = id;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }
}

internal sealed class RequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public string? Ip => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId => accessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string;
}
