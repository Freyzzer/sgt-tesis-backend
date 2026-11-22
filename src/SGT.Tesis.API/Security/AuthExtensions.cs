using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SGT.Tesis.API.Middleware;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Identity;

namespace SGT.Tesis.API.Security;

public static class Politicas
{
    public const string Administrador = nameof(Administrador);
    public const string LimiteLogin = nameof(LimiteLogin);
}

public static class AuthExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IRequestContext, RequestContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configuración diferida: usa los valores finales de Jwt (secretos incluidos).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var o = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = o.Issuer,
                    ValidateAudience = true,
                    ValidAudience = o.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(o.ClockSkewSeconds),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };

                // Una cuenta desactivada o con rol cambiado pierde el acceso de inmediato,
                // sin esperar a que venza el access token.
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                        var sub = context.Principal?.FindFirst("sub")?.Value;
                        var usuario = Guid.TryParse(sub, out var id)
                            ? await usuarios.ObtenerPorIdAsync(id, context.HttpContext.RequestAborted)
                            : null;

                        if (usuario is null || !usuario.Activo
                            || usuario.Rol.ToString() != context.Principal?.FindFirst("role")?.Value)
                        {
                            context.Fail("Sesión no vigente.");
                        }
                        else if (usuario.DebeCambiarPassword && context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identidad)
                        {
                            // La contraseña es temporal: el middleware limita la API hasta que la cambie.
                            identidad.AddClaim(new System.Security.Claims.Claim(CambioPasswordObligatorioMiddleware.ClaimPendiente, "1"));
                        }
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Administrador, policy => policy.RequireRole(nameof(Rol.Administrador)));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // El límite se lee en tiempo de solicitud, con la configuración final del ambiente.
            options.AddPolicy(Politicas.LimiteLogin, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimit:LoginPermitPerMinute", 10),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
