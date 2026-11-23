using FluentValidation;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.GestionTesis;

/// <summary>El identificador del estudiante nunca viaja en el cuerpo: se toma del usuario autenticado.</summary>
public sealed record RegistrarTesisCommand(string Titulo, string Resumen);

public sealed class RegistrarTesisCommandValidator : AbstractValidator<RegistrarTesisCommand>
{
    public RegistrarTesisCommandValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(TesisGrado.TituloMaxLength);
        RuleFor(x => x.Resumen).NotEmpty().MaximumLength(TesisGrado.ResumenMaxLength);
    }
}

public sealed class RegistrarTesisHandler(
    ITesisRepository tesis,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit) : ICommandHandler<RegistrarTesisCommand, TesisDto>
{
    public async Task<TesisDto> HandleAsync(RegistrarTesisCommand command, CancellationToken cancellationToken)
    {
        var studentId = AutorizacionTesis.ExigirEstudiante(actual);

        if (await tesis.ExisteActivaAsync(studentId, cancellationToken))
        {
            throw new ConflictoException("Ya tiene una tesis en curso.");
        }

        var nueva = TesisGrado.Crear(studentId, command.Titulo, command.Resumen, clock.UtcNow);
        tesis.Agregar(nueva);
        audit.Registrar(AccionesAuditoria.TesisRegistrada, "Tesis", nueva.Id, nueva.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TesisDto.Desde(nueva);
    }
}
