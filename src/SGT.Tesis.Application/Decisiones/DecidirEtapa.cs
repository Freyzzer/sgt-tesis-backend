using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Decisiones;

public sealed record DecidirEtapaCommand(Guid TesisId, int Etapa, ResultadoDecision Resultado, string? Justificacion);

public sealed class DecidirEtapaCommandValidator : AbstractValidator<DecidirEtapaCommand>
{
    public DecidirEtapaCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.Resultado).IsInEnum();
        RuleFor(x => x.Justificacion).MaximumLength(Decision.JustificacionMaxLength);
        RuleFor(x => x.Justificacion)
            .NotEmpty().WithMessage("La justificación es obligatoria al observar o rechazar una etapa.")
            .When(x => x.Resultado is ResultadoDecision.Observada or ResultadoDecision.Rechazada);
    }
}

/// <summary>
/// Única vía para cambiar el resultado de una etapa. La decisión y el cambio de estado se guardan en la
/// misma transacción; la concurrencia optimista de la etapa impide dos decisiones simultáneas (409).
/// </summary>
public sealed class DecidirEtapaHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IUsuarioRepository usuarios,
    IPoliticasDeEtapa politicas,
    INotificador notificador,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<DecidirEtapaHandler> logger) : ICommandHandler<DecidirEtapaCommand, TesisDto>
{
    public async Task<TesisDto> HandleAsync(DecidirEtapaCommand command, CancellationToken cancellationToken)
    {
        var adminId = AutorizacionTesis.ExigirAdministrador(actual);

        var encontrada = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var tipo = (EtapaTipo)command.Etapa;
        var politica = politicas.Para(tipo);
        var decision = encontrada.Decidir(tipo, command.Resultado, adminId, command.Justificacion, clock.UtcNow, politica);

        decisiones.Agregar(decision);

        if (await usuarios.ObtenerPorIdAsync(encontrada.StudentId, cancellationToken) is { } estudiante)
        {
            var (asunto, cuerpo) = Plantillas.Decision(tipo, command.Resultado, politica.PermiteReintento);
            notificador.Encolar(estudiante.Correo, asunto, cuerpo);
        }

        audit.Registrar(AccionesAuditoria.DecisionRegistrada, "Decision", decision.Id, encontrada.Id, $"{decision.Resultado} en la etapa {tipo}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Decisión {Resultado} sobre la etapa {Etapa} de la tesis {TesisId} por {ActorId}; tesis {EstadoTesis}",
            decision.Resultado,
            tipo,
            encontrada.Id,
            adminId,
            encontrada.Estado);

        return TesisDto.Desde(encontrada, await decisiones.ListarPorTesisAsync(encontrada.Id, cancellationToken));
    }
}
