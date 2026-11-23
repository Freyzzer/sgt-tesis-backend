using Microsoft.AspNetCore.Identity;
using SGT.Tesis.Application.Common.Abstractions;

namespace SGT.Tesis.Infrastructure.Identity;

/// <summary>PBKDF2 (HMAC-SHA512, sal aleatoria e iteraciones configuradas por ASP.NET Identity).</summary>
internal sealed class PasswordHasherService : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Contexto = new();
    private static readonly Lazy<string> HashDeRelleno = new(() => Hasher.HashPassword(Contexto, Guid.NewGuid().ToString("N")));

    public string Hash(string password) => Hasher.HashPassword(Contexto, password);

    public bool Verify(string password, string hash) =>
        Hasher.VerifyHashedPassword(Contexto, hash, password) is not PasswordVerificationResult.Failed;

    public void SimularVerificacion(string password) =>
        Hasher.VerifyHashedPassword(Contexto, HashDeRelleno.Value, password);
}
