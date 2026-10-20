using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure layer services in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure dependencies, including the database context, with the service collection.
    /// </summary>
    /// <param name="services">The service collection to register dependencies into.</param>
    /// <param name="configuration">The application configuration containing connection strings and setting definitions.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SistemaPosgradosDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(SistemaPosgradosDbContext).Assembly.FullName)));

        return services;
    }
}
