using System.Net;

namespace SGT.Tesis.API.Extensions;

public static class StartupChecks
{
    public const string SeccionProxies = "ForwardedHeaders";

    /// <summary>
    /// Fuera de Development, <c>AllowedHosts</c> no puede ser un comodín: debe listar los hosts
    /// públicos de la API (variable de entorno <c>AllowedHosts</c>).
    /// </summary>
    public static void EnsureAllowedHosts(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        var hosts = configuration["AllowedHosts"];

        if (string.IsNullOrWhiteSpace(hosts) || hosts.Split(';', StringSplitOptions.TrimEntries).Contains("*"))
        {
            throw new InvalidOperationException(
                "AllowedHosts no puede ser '*' ni estar vacío fuera de Development. " +
                "Configúrelo con los hosts de la API (variable de entorno AllowedHosts, separados por ';').");
        }
    }

    /// <summary>
    /// Si la API corre detrás de un proxy inverso (<c>ForwardedHeaders:Habilitado=true</c>), exige indicar las redes
    /// del proxy en <c>ForwardedHeaders:RedesConfiables</c> (CIDR) para no aceptar X-Forwarded-* de cualquiera.
    /// Devuelve <c>null</c> si no está habilitado.
    /// </summary>
    public static IReadOnlyList<IPNetwork>? LeerProxiesConfiables(IConfiguration configuration)
    {
        var seccion = configuration.GetSection(SeccionProxies);

        if (!seccion.GetValue<bool>("Habilitado"))
        {
            return null;
        }

        var redes = seccion.GetSection("RedesConfiables").Get<string[]>() ?? [];

        if (redes.Length == 0)
        {
            throw new InvalidOperationException(
                "ForwardedHeaders:Habilitado=true exige ForwardedHeaders:RedesConfiables (CIDR del proxy, p. ej. 10.0.0.0/8).");
        }

        var resultado = new List<IPNetwork>();

        foreach (var red in redes)
        {
            if (!IPNetwork.TryParse(red.Trim(), out var parseada))
            {
                throw new InvalidOperationException($"ForwardedHeaders:RedesConfiables contiene un CIDR inválido: '{red}'.");
            }

            resultado.Add(parseada);
        }

        return resultado;
    }

    /// <summary>Consulta /health/live del propio proceso (para HEALTHCHECK de Docker). Devuelve 0 si está sano.</summary>
    public static async Task<int> EjecutarHealthcheckAsync()
    {
        var puerto = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080").Split(';')[0];
        var host = (Environment.GetEnvironmentVariable("AllowedHosts") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(h => !h.Contains('*'));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{puerto}/health/live");

        // El filtro de hosts rechaza "127.0.0.1": se envía el host público configurado.
        if (host is not null)
        {
            solicitud.Headers.Host = host;
        }

        try
        {
            using var respuesta = await http.SendAsync(solicitud);
            return respuesta.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
