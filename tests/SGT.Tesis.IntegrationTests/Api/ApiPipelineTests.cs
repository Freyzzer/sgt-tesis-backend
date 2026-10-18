using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Api;

public class ApiPipelineTests : IClassFixture<ApiFactory>
{
    private const string AllowedOrigin = "http://frontend.test";

    private readonly HttpClient _client;

    public ApiPipelineTests(ApiFactory factory) => _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    [Trait("Issue", "23")]
    public async Task Health_live_responde_200()
    {
        var response = await _client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task Toda_respuesta_incluye_cabeceras_de_seguridad()
    {
        var response = await _client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task Ruta_inexistente_devuelve_problem_details()
    {
        var response = await _client.GetAsync("/no-existe", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task Cors_permite_origen_en_lista_blanca()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    [Trait("Issue", "23")]
    public async Task Cors_no_permite_origen_fuera_de_la_lista()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "http://sitio-malicioso.test");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
