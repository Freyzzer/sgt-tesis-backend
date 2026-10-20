using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.GestionTesis;

public sealed record EnviarEtapaARevisionCommand(Guid TesisId, int Etapa);

public sealed class EnviarEtapaARevisionCommandValidator : AbstractValidator<EnviarEtapaARevisionCommand>
{
    public EnviarEtapaARevisionCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
    }
}

public sealed class EnviarEtapaARevisionHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IUsuarioRepository usuarios,
    IAsignacionRepository asignaciones,
    INotificador notificador,
    IRequisitosDocumentales requisitos,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<EnviarEtapaARevisionHandler> logger) : ICommandHandler<EnviarEtapaARevisionCommand, TesisDto>
{
    public async Task<TesisDto> HandleAsync(EnviarEtapaARevisionCommand command, CancellationToken cancellationToken)
    {
        AutorizacionTesis.ExigirEstudiante(actual);

        var propia = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        AutorizacionTesis.ExigirVisible(propia, actual);

        var tipo = (EtapaTipo)command.Etapa;
        var etapa = propia.ObtenerEtapa(tipo);

        var faltantes = await requisitos.ObtenerFaltantesAsync(etapa.Id, tipo, cancellationToken);

        if (faltantes.Count > 0)
        {
            throw new EtapaIncompletaException(faltantes);
        }

        propia.EnviarEtapaARevision(tipo, clock.UtcNow);
        var ronda = etapa.Ronda;

        var (asunto, cuerpo) = Plantillas.EtapaEnRevision(tipo);

        foreach (var administrador in await usuarios.ListarAdministradoresActivosAsync(cancellationToken))
        {
            notificador.Encolar(administrador.Correo, asunto, cuerpo);
        }

        // Cada envío abre una ronda: los evaluadores asignados deben evaluar (o volver a evaluar) la versión nueva.
        var (asuntoEvaluador, cuerpoEvaluador) = Plantillas.EtapaParaEvaluar(tipo, ronda);

        foreach (var asignacion in (await asignaciones.ListarPorEtapaAsync(etapa.Id, cancellationToken)).Where(a => a.Activa))
        {
            if (await usuarios.ObtenerPorIdAsync(asignacion.EvaluadorId, cancellationToken) is { Activo: true } evaluador)
            {
                notificador.Encolar(evaluador.Correo, asuntoEvaluador, cuerpoEvaluador);
            }
        }

        audit.Registrar(AccionesAuditoria.EtapaEnviada, "Etapa", etapa.Id, propia.Id, $"Etapa {tipo}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Etapa {Etapa} de la tesis {TesisId} enviada a revisión por {ActorId}", tipo, propia.Id, actual.UserId);

        return TesisDto.Desde(propia, await decisiones.ListarPorTesisAsync(propia.Id, cancellationToken));
    }
}
