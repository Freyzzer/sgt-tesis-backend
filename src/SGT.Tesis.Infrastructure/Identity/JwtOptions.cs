using System.Text;
using Microsoft.Extensions.Options;

namespace SGT.Tesis.Infrastructure.Identity;

/// <summary>
/// Parámetros de emisión de tokens. <see cref="SigningKey"/> es un secreto: se entrega por variable
/// de entorno (<c>Jwt__SigningKey</c>), user-secrets o gestor de secretos; nunca se versiona.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public int ClockSkewSeconds { get; set; } = 60;
}

internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add("Jwt:Issuer es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add("Jwt:Audience es obligatorio.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) < JwtOptions.MinSigningKeyBytes)
        {
            errors.Add($"Jwt:SigningKey es obligatoria y debe tener al menos {JwtOptions.MinSigningKeyBytes} bytes. Configúrela por variable de entorno (Jwt__SigningKey) o user-secrets.");
        }

        if (options.AccessTokenMinutes is < 1 or > 60)
        {
            errors.Add("Jwt:AccessTokenMinutes debe estar entre 1 y 60.");
        }

        if (options.RefreshTokenDays is < 1 or > 30)
        {
            errors.Add("Jwt:RefreshTokenDays debe estar entre 1 y 30.");
        }

        if (options.ClockSkewSeconds is < 0 or > 120)
        {
            errors.Add("Jwt:ClockSkewSeconds debe estar entre 0 y 120.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
