using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Usuarios;

public sealed record CambiarEstadoUsuarioCommand(Guid Id, bool Activo);

public sealed class CambiarEstadoUsuarioHandler(
    IUsuarioRepository usuarios,
    IRefreshTokenRepository refreshTokens,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    ILogger<CambiarEstadoUsuarioHandler> logger) : ICommandHandler<CambiarEstadoUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> HandleAsync(CambiarEstadoUsuarioCommand command, CancellationToken cancellationToken)
    {
        AutorizacionAdministrador.Exigir(actual);

        var usuario = await usuarios.ObtenerPorIdAsync(command.Id, cancellationToken)
            ?? throw new NoEncontradoException("El usuario no existe.");

        if (command.Activo)
        {
            usuario.Activar();
        }
        else
        {
            if (usuario.Id == actual.UserId)
            {
                throw new ConflictoException("No puede desactivar su propia cuenta.");
            }

            if (usuario.Rol == Rol.Administrador && usuario.Activo
                && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
            {
                throw new ConflictoException("No se puede desactivar al último Administrador activo.");
            }

            usuario.Desactivar();

            // Cierra todas las sesiones: los refresh tokens vigentes quedan revocados.
            var ahora = clock.UtcNow;

            foreach (var token in await refreshTokens.ObtenerActivosDeUsuarioAsync(usuario.Id, ahora, cancellationToken))
            {
                token.Revocar(ahora);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuario {UsuarioId} {Accion} por {ActorId}", usuario.Id, command.Activo ? "activado" : "desactivado", actual.UserId);

        return UsuarioDto.Desde(usuario);
    }
}

public sealed record CambiarRolUsuarioCommand(Guid Id, Rol Rol);

public sealed class CambiarRolUsuarioHandler(
    IUsuarioRepository usuarios,
    ICurrentUser actual,
    IUnitOfWork unitOfWork,
    ILogger<CambiarRolUsuarioHandler> logger) : ICommandHandler<CambiarRolUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> HandleAsync(CambiarRolUsuarioCommand command, CancellationToken cancellationToken)
    {
        AutorizacionAdministrador.Exigir(actual);

        var usuario = await usuarios.ObtenerPorIdAsync(command.Id, cancellationToken)
            ?? throw new NoEncontradoException("El usuario no existe.");

        if (usuario.Rol == command.Rol)
        {
            return UsuarioDto.Desde(usuario);
        }

        if (usuario.Id == actual.UserId)
        {
            throw new ConflictoException("No puede cambiar su propio rol.");
        }

        if (usuario.Rol == Rol.Administrador && usuario.Activo
            && await usuarios.ContarAdministradoresActivosAsync(cancellationToken) <= 1)
        {
            throw new ConflictoException("No se puede quitar el rol al último Administrador activo.");
        }

        var anterior = usuario.Rol;
        usuario.CambiarRol(command.Rol);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Rol del usuario {UsuarioId} cambiado de {Anterior} a {Nuevo} por {ActorId}", usuario.Id, anterior, usuario.Rol, actual.UserId);

        return UsuarioDto.Desde(usuario);
    }
}
