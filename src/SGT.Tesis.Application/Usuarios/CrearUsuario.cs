using FluentValidation;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Usuarios;

public sealed record CrearUsuarioCommand(string Nombres, string Apellidos, string Correo, string Password, Rol Rol);

public sealed class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioCommandValidator()
    {
        RuleFor(x => x.Nombres).NotEmpty().MaximumLength(Usuario.NombresMaxLength);
        RuleFor(x => x.Apellidos).NotEmpty().MaximumLength(Usuario.ApellidosMaxLength);
        RuleFor(x => x.Correo).NotEmpty().MaximumLength(Usuario.CorreoMaxLength).EmailAddress();
        RuleFor(x => x.Password).CumplePolitica();
        RuleFor(x => x.Rol).IsInEnum();
    }
}

public sealed class CrearUsuarioHandler(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    ICurrentUser actual,
    IUnitOfWork unitOfWork,
    IAuditTrail audit) : ICommandHandler<CrearUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> HandleAsync(CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        AutorizacionAdministrador.Exigir(actual);

        var correo = Usuario.NormalizarCorreo(command.Correo);

        if (await usuarios.ExisteCorreoAsync(correo, cancellationToken))
        {
            throw new ConflictoException("Ya existe un usuario con ese correo.");
        }

        var usuario = Usuario.Crear(command.Nombres, command.Apellidos, correo, hasher.Hash(command.Password), command.Rol);
        usuarios.Agregar(usuario);
        audit.Registrar(AccionesAuditoria.UsuarioCreado, "Usuario", usuario.Id, detalle: $"Rol {usuario.Rol}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UsuarioDto.Desde(usuario);
    }
}
