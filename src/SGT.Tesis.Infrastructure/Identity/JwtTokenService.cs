using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Infrastructure.Identity;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CrearAccessToken(Usuario usuario)
    {
        var expira = clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = clock.UtcNow,
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            // Solo identificador y rol: ningún dato personal en el token.
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                ["role"] = usuario.Rol.ToString(),
            },
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }

    public RefreshTokenGenerado GenerarRefreshToken()
    {
        var valor = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return new RefreshTokenGenerado(valor, HashRefreshToken(valor));
    }

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
