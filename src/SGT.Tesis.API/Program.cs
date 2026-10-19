using Serilog;
using SGT.Tesis.API.Endpoints;
using SGT.Tesis.API.Extensions;
using SGT.Tesis.API.Middleware;
using SGT.Tesis.API.Security;
using SGT.Tesis.Application;
using SGT.Tesis.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

StartupChecks.EnsureAllowedHosts(builder.Environment, builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApiCors();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapTesisEndpoints();
app.MapDocumentosEndpoints();
app.MapEvaluacionEndpoints();
app.MapAuditoriaEndpoints();
app.MapDashboardEndpoints();

app.Run();

public partial class Program;
