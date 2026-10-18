using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SGT.Tesis.API.Middleware;
using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.IntegrationTests.Api;

public class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body, string Raw)> Ejecutar(Exception exception)
    {
        var services = new ServiceCollection().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "traza-123";

        var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        var raw = await new StreamReader(context.Response.Body).ReadToEndAsync();

        return (context.Response.StatusCode, JsonDocument.Parse(raw).RootElement, raw);
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task DomainException_devuelve_422_con_el_mensaje_de_negocio()
    {
        var (status, body, _) = await Ejecutar(new DomainException("Titulo es obligatorio."));

        Assert.Equal(422, status);
        Assert.Equal("Titulo es obligatorio.", body.GetProperty("detail").GetString());
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task Excepcion_inesperada_devuelve_500_sin_filtrar_detalles_internos()
    {
        var (status, body, raw) = await Ejecutar(new InvalidOperationException("cadena secreta Server=interno"));

        Assert.Equal(500, status);
        Assert.DoesNotContain("secreta", raw);
        Assert.DoesNotContain("InvalidOperationException", raw);
        Assert.Equal("traza-123", body.GetProperty("traceId").GetString());
    }
}
