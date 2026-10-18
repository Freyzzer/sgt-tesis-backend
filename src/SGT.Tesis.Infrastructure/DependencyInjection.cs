using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Configure(o => o.ConnectionString =
                configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty)
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>((provider, builder) =>
        {
            var db = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            builder.UseSqlServer(db.ConnectionString, sql =>
            {
                sql.CommandTimeout(db.CommandTimeoutSeconds);
                sql.EnableRetryOnFailure(
                    db.MaxRetryCount,
                    TimeSpan.FromSeconds(db.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            });
        });

        return services;
    }
}
