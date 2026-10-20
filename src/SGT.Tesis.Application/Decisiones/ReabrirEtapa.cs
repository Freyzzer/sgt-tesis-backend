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

public sealed record ReabrirEtapaCommand(Guid TesisId, int Etapa, string? Justificacion);

public sealed class ReabrirEtapaCommandValidator : AbstractValidator<ReabrirEtapaCommand>
{
    public ReabrirEtapaCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.Justificacion)
            .NotEmpty().WithMessage("La justificación es obligatoria al reabrir una etapa.")
            .MaximumLength(Decision.JustificacionMaxLength);
    }
}

/// <summary>
/// Devuelve al estudiante una etapa rechazada (también una definitiva) para corregir errores sin tocar la base de datos
/// (ADR-005). Queda como una decisión inmutable (<c>Reabierta</c>) con su justificación y en la auditoría.
/// </summary>
public sealed class ReabrirEtapaHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IUsuarioRepository usuarios,
    INotificador notificador,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<ReabrirEtapaHandler> logger) : ICommandHandler<ReabrirEtapaCommand, TesisDto>
{
    public async Task<TesisDto> HandleAsync(ReabrirEtapaCommand command, CancellationToken cancellationToken)
    {
        var adminId = AutorizacionTesis.ExigirAdministrador(actual);

        var encontrada = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var tipo = (EtapaTipo)command.Etapa;

        // Un estudiante solo puede tener una tesis en curso: si ya registró otra tras el rechazo, no se puede reabrir esta.
        if (encontrada.Estado == EstadoTesis.Rechazada && await tesis.ExisteActivaAsync(encontrada.StudentId, cancellationToken))
        {
            throw new ConflictoException("El estudiante ya tiene otra tesis en curso; no se puede reabrir esta mientras tanto.");
        }

        var decision = encontrada.Reabrir(tipo, adminId, command.Justificacion, clock.UtcNow);
        decisiones.Agregar(decision);

        if (await usuarios.ObtenerPorIdAsync(encontrada.StudentId, cancellationToken) is { } estudiante)
        {
            var (asunto, cuerpo) = Plantillas.EtapaReabierta(tipo);
            notificador.Encolar(estudiante.Correo, asunto, cuerpo);
        }

        audit.Registrar(AccionesAuditoria.EtapaReabierta, "Decision", decision.Id, encontrada.Id, $"Etapa {tipo} reabierta.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Etapa {Etapa} de la tesis {TesisId} reabierta por {ActorId}", tipo, encontrada.Id, adminId);

        return TesisDto.Desde(encontrada, await decisiones.ListarPorTesisAsync(encontrada.Id, cancellationToken));
    }
}
