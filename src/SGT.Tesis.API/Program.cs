using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SGT.Tesis.API.Endpoints;
using SGT.Tesis.API.Extensions;
using SGT.Tesis.API.Health;
using SGT.Tesis.API.Middleware;
using SGT.Tesis.API.Security;
using SGT.Tesis.Application;
using SGT.Tesis.Infrastructure;

// Modo de verificación para HEALTHCHECK de contenedores (las imágenes no traen curl):
// `dotnet SGT.Tesis.API.dll --healthcheck` consulta /health/live y devuelve 0 si responde.
if (args.Contains("--healthcheck"))
{
    return await StartupChecks.EjecutarHealthcheckAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

StartupChecks.EnsureAllowedHosts(builder.Environment, builder.Configuration);
var redesConfiables = StartupChecks.LeerProxiesConfiables(builder.Configuration);

if (redesConfiables is not null)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var red in redesConfiables)
        {
            options.KnownIPNetworks.Add(red);
        }
    });
}

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApiCors();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<StorageHealthCheck>("storage", tags: ["ready"]);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Detrás de un proxy inverso: solo se confía en las cabeceras X-Forwarded-* de las redes configuradas.
if (redesConfiables is not null)
{
    app.UseForwardedHeaders();
}

// El log de peticiones va por fuera del manejador de excepciones para registrar el estado final.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CambioPasswordObligatorioMiddleware>();
app.UseAuthorization();

// Liveness: el proceso responde. Readiness: además la base de datos y el almacenamiento están disponibles.
// Las respuestas no revelan detalles internos; los motivos quedan en el log.
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    },
});

app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapTesisEndpoints();
app.MapDocumentosEndpoints();
app.MapEvaluacionEndpoints();
app.MapAuditoriaEndpoints();
app.MapDashboardEndpoints();

app.Run();
return 0;

public partial class Program;
