using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Documentos;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Infrastructure.Documentos;
using SGT.Tesis.Infrastructure.Identity;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.Infrastructure.Persistence.Repositories;

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

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<BootstrapOptions>().Bind(configuration.GetSection(BootstrapOptions.SectionName));

        services.AddScoped<AuditoriaInterceptor>();

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
            builder.AddInterceptors(provider.GetRequiredService<AuditoriaInterceptor>());
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITesisRepository, TesisRepository>();
        services.AddSingleton<IValidateOptions<DocumentosOptions>, DocumentosOptionsValidator>();
        services.AddOptions<DocumentosOptions>().Bind(configuration.GetSection(DocumentosOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.SectionName)).ValidateOnStart();

        services.AddSingleton<IDocumentosPolicy, DocumentosPolicy>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<IRequisitosDocumentales, RequisitosDocumentalesConfigurables>();

        services.AddHostedService<AdminBootstrapService>();

        return services;
    }
}
