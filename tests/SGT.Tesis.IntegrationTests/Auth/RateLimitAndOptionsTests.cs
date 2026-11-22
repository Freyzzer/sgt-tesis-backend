using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Infrastructure.Identity;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Auth;

public class RateLimitAndOptionsTests
{
    [Fact]
    [Trait("NFR", "04")]
    public async Task El_login_aplica_limite_de_solicitudes_por_minuto()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["RateLimit:LoginPermitPerMinute"] = "3";
        using var client = factory.CreateClient();

        var estados = new List<HttpStatusCode>();

        for (var i = 0; i < 5; i++)
        {
            estados.Add((await client.PostAsJsonAsync("/api/v1/auth/login", new { correo = "a@b.edu", password = "x" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        }

        Assert.Equal(3, estados.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, estados.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    [Trait("NFR", "03")]
    public void Sin_clave_de_firma_la_configuracion_jwt_es_invalida()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Jwt:SigningKey"] = "corta";

        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            using var scope = factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        });

        Assert.Contains("SigningKey", ex.Message);
    }
}
