using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Contrasenas;

internal static class SesionesDeUsuario
{
    /// <summary>Cierra todas las sesiones del usuario: revoca sus refresh tokens vigentes.</summary>
    public static async Task RevocarTodasAsync(IRefreshTokenRepository refreshTokens, Guid usuarioId, DateTime ahora, CancellationToken cancellationToken)
    {
        foreach (var token in await refreshTokens.ObtenerActivosDeUsuarioAsync(usuarioId, ahora, cancellationToken))
        {
            token.Revocar(ahora);
        }
    }
}

// ---------- Cambio de contraseña (usuario autenticado) ----------

public sealed record CambiarPasswordCommand(string PasswordActual, string PasswordNueva);

public sealed class CambiarPasswordCommandValidator : AbstractValidator<CambiarPasswordCommand>
{
    public CambiarPasswordCommandValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PasswordNueva).CumplePolitica();
        RuleFor(x => x.PasswordNueva).NotEqual(x => x.PasswordActual).WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}

/// <summary>
/// Cambia la contraseña verificando la actual (con el mismo bloqueo por intentos fallidos que el login),
/// cierra todas las sesiones y avisa por correo.
/// </summary>
public sealed class CambiarPasswordHandler(
    IUsuarioRepository usuarios,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    INotificador notificador,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<CambiarPasswordHandler> logger) : ICommandHandler<CambiarPasswordCommand, bool>
{
    public async Task<bool> HandleAsync(CambiarPasswordCommand command, CancellationToken cancellationToken)
    {
        var usuarioId = actual.UserId ?? throw new NoAutenticadoException();
        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);

        if (usuario is not { Activo: true })
        {
            throw new NoAutenticadoException();
        }

        var ahora = clock.UtcNow;

        if (usuario.EstaBloqueado(ahora))
        {
            throw new NoAutenticadoException();
        }

        if (!hasher.Verify(command.PasswordActual, usuario.PasswordHash))
        {
            var bloqueada = usuario.RegistrarFalloDeAcceso(ahora);
            audit.Registrar(AccionesAuditoria.LoginFallido, "Usuario", usuario.Id, detalle: "Contraseña actual incorrecta al cambiarla.");

            if (bloqueada)
            {
                audit.Registrar(AccionesAuditoria.CuentaBloqueada, "Usuario", usuario.Id, detalle: $"Bloqueada hasta {usuario.BloqueadoHasta:O}.");
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new ValidacionException(new Dictionary<string, string[]> { ["passwordActual"] = ["La contraseña actual no es correcta."] });
        }

        usuario.CambiarPassword(hasher.Hash(command.PasswordNueva));
        await SesionesDeUsuario.RevocarTodasAsync(refreshTokens, usuario.Id, ahora, cancellationToken);

        var (asunto, cuerpo) = Plantillas.PasswordCambiada();
        notificador.Encolar(usuario.Correo, asunto, cuerpo);
        audit.Registrar(AccionesAuditoria.PasswordCambiada, "Usuario", usuario.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("El usuario {UsuarioId} cambió su contraseña", usuario.Id);

        return true;
    }
}

// ---------- Solicitud de restablecimiento (anónimo) ----------

public sealed record OlvidePasswordCommand(string Correo);

public sealed class OlvidePasswordCommandValidator : AbstractValidator<OlvidePasswordCommand>
{
    public OlvidePasswordCommandValidator()
    {
        RuleFor(x => x.Correo).NotEmpty().MaximumLength(Usuario.CorreoMaxLength).EmailAddress();
    }
}

/// <summary>
/// Genera un token de un solo uso y lo envía por correo. La respuesta es la misma exista o no la cuenta (no revela
/// qué correos están registrados). Máximo una solicitud por minuto y cuenta para no usarlo como arma de correo masivo.
/// </summary>
public sealed class OlvidePasswordHandler(
    IUsuarioRepository usuarios,
    IPasswordResetRepository resets,
    ITokenService tokens,
    INotificador notificador,
    IAppLinks enlaces,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<OlvidePasswordHandler> logger) : ICommandHandler<OlvidePasswordCommand, bool>
{
    private static readonly TimeSpan EsperaMinima = TimeSpan.FromMinutes(1);

    public async Task<bool> HandleAsync(OlvidePasswordCommand command, CancellationToken cancellationToken)
    {
        var ahora = clock.UtcNow;
        var usuario = await usuarios.ObtenerPorCorreoAsync(Usuario.NormalizarCorreo(command.Correo), cancellationToken);

        if (usuario is not { Activo: true })
        {
            logger.LogInformation("Solicitud de restablecimiento para un correo no registrado o inactivo");
            return true;
        }

        var vigentes = await resets.ObtenerVigentesDeUsuarioAsync(usuario.Id, ahora, cancellationToken);

        if (vigentes.Any(t => t.ExpiraEn - PasswordResetToken.Vigencia > ahora - EsperaMinima))
        {
            return true;
        }

        // Un solo enlace válido a la vez: los anteriores quedan invalidados.
        foreach (var anterior in vigentes)
        {
            anterior.MarcarUsado(ahora);
        }

        var nuevo = tokens.GenerarTokenOpaco();
        resets.Agregar(PasswordResetToken.Crear(usuario.Id, nuevo.Hash, ahora));

        var (asunto, cuerpo) = Plantillas.RestablecerPassword(enlaces.UrlRestablecerPassword, nuevo.Valor);
        notificador.Encolar(usuario.Correo, asunto, cuerpo, sensible: true);
        audit.Registrar(AccionesAuditoria.PasswordRestablecimientoSolicitado, "Usuario", usuario.Id, actorId: usuario.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

// ---------- Restablecimiento con el token (anónimo) ----------

public sealed record RestablecerPasswordCommand(string Token, string PasswordNueva);

public sealed class RestablecerPasswordCommandValidator : AbstractValidator<RestablecerPasswordCommand>
{
    public RestablecerPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PasswordNueva).CumplePolitica();
    }
}

public sealed class RestablecerPasswordHandler(
    IUsuarioRepository usuarios,
    IPasswordResetRepository resets,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IPasswordHasher hasher,
    INotificador notificador,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit) : ICommandHandler<RestablecerPasswordCommand, bool>
{
    public async Task<bool> HandleAsync(RestablecerPasswordCommand command, CancellationToken cancellationToken)
    {
        var ahora = clock.UtcNow;
        var token = await resets.ObtenerPorHashAsync(tokens.HashTokenOpaco(command.Token), cancellationToken);
        var usuario = token is { } t && t.EstaVigente(ahora) ? await usuarios.ObtenerPorIdAsync(t.UsuarioId, cancellationToken) : null;

        if (token is null || usuario is not { Activo: true })
        {
            throw new ValidacionException(new Dictionary<string, string[]> { ["token"] = ["El enlace no es válido o ya venció. Solicite uno nuevo."] });
        }

        usuario.CambiarPassword(hasher.Hash(command.PasswordNueva));
        token.MarcarUsado(ahora);
        await SesionesDeUsuario.RevocarTodasAsync(refreshTokens, usuario.Id, ahora, cancellationToken);

        var (asunto, cuerpo) = Plantillas.PasswordCambiada();
        notificador.Encolar(usuario.Correo, asunto, cuerpo);
        audit.Registrar(AccionesAuditoria.PasswordRestablecida, "Usuario", usuario.Id, actorId: usuario.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
