using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace SGT.Tesis.API.Extensions;

public static class CorsExtensions
{
    public const string SectionName = "Cors:AllowedOrigins";

    /// <summary>
    /// CORS por lista blanca de orígenes por ambiente. Si la lista está vacía no se permite
    /// ningún origen externo. La configuración se lee de forma diferida, con los valores finales.
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var origins = configuration.GetSection(SectionName).Get<string[]>() ?? [];

            if (origins.Any(o => o.Trim() == "*"))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins no admite '*'. Liste los orígenes explícitamente.");
            }

            options.AddDefaultPolicy(policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                        .WithHeaders("Authorization", "Content-Type");
                }
            });
        });

        return services;
    }
}
