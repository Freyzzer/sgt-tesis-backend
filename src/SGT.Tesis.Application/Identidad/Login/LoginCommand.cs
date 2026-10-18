using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Identidad.Dtos;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Identidad.Login;

public sealed record LoginCommand(string Correo, string Password);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Correo).NotEmpty().MaximumLength(Usuario.CorreoMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginHandler(
    IUsuarioRepository usuarios,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    ITokenService tokens,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger<LoginHandler> logger) : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var ahora = clock.UtcNow;
        var usuario = await usuarios.ObtenerPorCorreoAsync(Usuario.NormalizarCorreo(command.Correo), cancellationToken);

        if (usuario is null)
        {
            hasher.SimularVerificacion(command.Password); // iguala el tiempo de respuesta cuando el correo no existe
            throw new NoAutenticadoException();
        }

        if (!usuario.Activo || usuario.EstaBloqueado(ahora))
        {
            logger.LogWarning("Intento de acceso rechazado para el usuario {UsuarioId} (inactivo o bloqueado)", usuario.Id);
            throw new NoAutenticadoException();
        }

        if (!hasher.Verify(command.Password, usuario.PasswordHash))
        {
            var bloqueada = usuario.RegistrarFalloDeAcceso(ahora);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (bloqueada)
            {
                logger.LogWarning("Cuenta del usuario {UsuarioId} bloqueada hasta {BloqueadoHasta} por intentos fallidos", usuario.Id, usuario.BloqueadoHasta);
            }
            else
            {
                logger.LogInformation("Credenciales inválidas para el usuario {UsuarioId}", usuario.Id);
            }

            throw new NoAutenticadoException();
        }

        usuario.RegistrarAccesoExitoso();

        var access = tokens.CrearAccessToken(usuario);
        var refresh = tokens.GenerarRefreshToken();
        var expira = ahora.Add(tokens.RefreshTokenLifetime);
        refreshTokens.Agregar(RefreshToken.Crear(usuario.Id, refresh.Hash, expira));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Inicio de sesión del usuario {UsuarioId}", usuario.Id);

        return new TokenResponse(access.Valor, access.ExpiraEn, refresh.Valor, expira);
    }
}
