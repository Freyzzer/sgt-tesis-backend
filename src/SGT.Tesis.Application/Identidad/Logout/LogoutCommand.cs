using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;

namespace SGT.Tesis.Application.Identidad.Logout;

public sealed record LogoutCommand(string RefreshToken);

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}

/// <summary>Cierra la sesión revocando toda la cadena del refresh token. Es idempotente.</summary>
public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IClock clock,
    IUnitOfWork unitOfWork) : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var token = await refreshTokens.ObtenerPorHashAsync(tokens.HashRefreshToken(command.RefreshToken), cancellationToken);

        if (token is null)
        {
            return false;
        }

        var ahora = clock.UtcNow;

        foreach (var miembro in await refreshTokens.ObtenerFamiliaAsync(token.FamiliaId, cancellationToken))
        {
            miembro.Revocar(ahora);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
