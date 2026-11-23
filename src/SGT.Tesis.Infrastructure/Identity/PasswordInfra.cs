using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.Infrastructure.Identity;

/// <summary>Direcciones del frontend para los correos. Se configuran por ambiente en la sección <c>Frontend</c>.</summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Plantilla de la pantalla de restablecimiento; debe contener <c>{token}</c>, p. ej.
    /// <c>https://app.institucion.edu/restablecer?token={token}</c>. Vacía: el correo incluye solo el código.
    /// </summary>
    public string? UrlRestablecerPassword { get; set; }
}

internal sealed class FrontendOptionsValidator : IValidateOptions<FrontendOptions>
{
    public ValidateOptionsResult Validate(string? name, FrontendOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UrlRestablecerPassword))
        {
            return ValidateOptionsResult.Success;
        }

        var plantilla = options.UrlRestablecerPassword;

        if (!plantilla.Contains("{token}", StringComparison.Ordinal)
            || !Uri.TryCreate(plantilla.Replace("{token}", "x"), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("https" or "http"))
        {
            return ValidateOptionsResult.Fail("Frontend:UrlRestablecerPassword debe ser una URL http(s) absoluta que contenga {token}.");
        }

        return ValidateOptionsResult.Success;
    }
}

internal sealed class AppLinks(IOptions<FrontendOptions> options) : IAppLinks
{
    public string? UrlRestablecerPassword => string.IsNullOrWhiteSpace(options.Value.UrlRestablecerPassword) ? null : options.Value.UrlRestablecerPassword;
}

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(PasswordResetToken.HashMaxLength);
        builder.Property(t => t.ExpiraEn).IsRequired();

        builder.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UsuarioId);
    }
}

internal sealed class PasswordResetRepository(AppDbContext db) : IPasswordResetRepository
{
    public Task<PasswordResetToken?> ObtenerPorHashAsync(string hash, CancellationToken cancellationToken) =>
        db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> ObtenerVigentesDeUsuarioAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken) =>
        await db.PasswordResetTokens
            .Where(t => t.UsuarioId == usuarioId && t.UsadoEn == null && t.ExpiraEn > utcNow)
            .ToListAsync(cancellationToken);

    public void Agregar(PasswordResetToken token) => db.PasswordResetTokens.Add(token);
}
