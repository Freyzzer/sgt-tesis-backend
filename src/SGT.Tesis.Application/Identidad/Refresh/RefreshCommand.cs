using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Identidad.Dtos;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Identidad.Refresh;

public sealed record RefreshCommand(string RefreshToken);

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}

public sealed class RefreshHandler(
    IUsuarioRepository usuarios,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<RefreshHandler> logger) : ICommandHandler<RefreshCommand, TokenResponse>
{
    public async Task<TokenResponse> HandleAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        var ahora = clock.UtcNow;
        var actual = await refreshTokens.ObtenerPorHashAsync(tokens.HashRefreshToken(command.RefreshToken), cancellationToken)
            ?? throw new NoAutenticadoException();

        if (actual.EstaRevocado)
        {
            // Reutilización de un token ya usado: se asume robo y se revoca toda la cadena.
            audit.Registrar(AccionesAuditoria.RefreshReutilizado, "Usuario", actual.UsuarioId, actorId: actual.UsuarioId);
            await RevocarFamiliaAsync(actual.FamiliaId, ahora, cancellationToken);
            logger.LogWarning("Reutilización de refresh token detectada para el usuario {UsuarioId}; cadena revocada", actual.UsuarioId);
            throw new NoAutenticadoException();
        }

        if (!actual.EstaActivo(ahora))
        {
            throw new NoAutenticadoException();
        }

        var usuario = await usuarios.ObtenerPorIdAsync(actual.UsuarioId, cancellationToken);

        if (usuario is null || !usuario.Activo)
        {
            await RevocarFamiliaAsync(actual.FamiliaId, ahora, cancellationToken);
            throw new NoAutenticadoException();
        }

        var access = tokens.CrearAccessToken(usuario);
        var nuevo = tokens.GenerarRefreshToken();
        var expira = ahora.Add(tokens.RefreshTokenLifetime);
        var siguiente = RefreshToken.Crear(usuario.Id, nuevo.Hash, expira, actual.FamiliaId);

        actual.Revocar(ahora, siguiente.Id);
        refreshTokens.Agregar(siguiente);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenResponse(access.Valor, access.ExpiraEn, nuevo.Valor, expira);
    }

    private async Task RevocarFamiliaAsync(Guid familiaId, DateTime ahora, CancellationToken cancellationToken)
    {
        foreach (var token in await refreshTokens.ObtenerFamiliaAsync(familiaId, cancellationToken))
        {
            token.Revocar(ahora);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
