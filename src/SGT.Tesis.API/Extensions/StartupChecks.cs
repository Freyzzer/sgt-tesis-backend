namespace SGT.Tesis.API.Extensions;

public static class StartupChecks
{
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
}
