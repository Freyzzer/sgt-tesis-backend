using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Evaluacion;

public sealed record AsignarEvaluadorCommand(Guid TesisId, int Etapa, Guid EvaluadorId);

public sealed class AsignarEvaluadorCommandValidator : AbstractValidator<AsignarEvaluadorCommand>
{
    public AsignarEvaluadorCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.EvaluadorId).NotEmpty();
    }
}

public sealed class AsignarEvaluadorHandler(
    ITesisRepository tesis,
    IUsuarioRepository usuarios,
    IAsignacionRepository asignaciones,
    INotificador notificador,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<AsignarEvaluadorHandler> logger) : ICommandHandler<AsignarEvaluadorCommand, AsignacionDto>
{
    public async Task<AsignacionDto> HandleAsync(AsignarEvaluadorCommand command, CancellationToken cancellationToken)
    {
        var adminId = AutorizacionTesis.ExigirAdministrador(actual);

        var encontrada = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)command.Etapa);

        if (!etapa.EstaAbierta)
        {
            throw new TransicionInvalidaException($"La etapa está {etapa.Estado}; ya no admite evaluadores.");
        }

        var evaluador = await usuarios.ObtenerPorIdAsync(command.EvaluadorId, cancellationToken);

        if (evaluador is not { Activo: true, Rol: Rol.Evaluador })
        {
            throw new DomainException("El usuario indicado no es un Evaluador activo.");
        }

        if (await asignaciones.ObtenerActivaAsync(etapa.Id, evaluador.Id, cancellationToken) is not null)
        {
            throw new ConflictoException("El evaluador ya está asignado a esta etapa.");
        }

        var asignacion = AsignacionEvaluador.Crear(etapa.Id, evaluador.Id, adminId, clock.UtcNow);
        asignaciones.Agregar(asignacion);
        var (asunto, cuerpo) = Plantillas.EvaluadorAsignado(etapa.Tipo);
        notificador.Encolar(evaluador.Correo, asunto, cuerpo);
        audit.Registrar(AccionesAuditoria.EvaluadorAsignado, "Asignacion", asignacion.Id, encontrada.Id, $"Evaluador {evaluador.Id}, etapa {etapa.Tipo}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Evaluador {EvaluadorId} asignado a la etapa {Etapa} de la tesis {TesisId} por {ActorId}", evaluador.Id, etapa.Tipo, encontrada.Id, adminId);

        return AsignacionDto.Desde(asignacion);
    }
}

public sealed record RetirarEvaluadorCommand(Guid TesisId, int Etapa, Guid EvaluadorId);

public sealed class RetirarEvaluadorHandler(
    ITesisRepository tesis,
    IAsignacionRepository asignaciones,
    ICurrentUser actual,
    IClock clock,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<RetirarEvaluadorHandler> logger) : ICommandHandler<RetirarEvaluadorCommand, bool>
{
    public async Task<bool> HandleAsync(RetirarEvaluadorCommand command, CancellationToken cancellationToken)
    {
        var adminId = AutorizacionTesis.ExigirAdministrador(actual);

        var encontrada = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)command.Etapa);
        var asignacion = await asignaciones.ObtenerActivaAsync(etapa.Id, command.EvaluadorId, cancellationToken)
            ?? throw new NoEncontradoException("El evaluador no tiene una asignación activa en esta etapa.");

        asignacion.Retirar(clock.UtcNow);
        audit.Registrar(AccionesAuditoria.EvaluadorRetirado, "Asignacion", asignacion.Id, encontrada.Id, $"Evaluador {command.EvaluadorId}, etapa {etapa.Tipo}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Evaluador {EvaluadorId} retirado de la etapa {Etapa} de la tesis {TesisId} por {ActorId}", command.EvaluadorId, etapa.Tipo, encontrada.Id, adminId);

        return true;
    }
}

public sealed record ListarAsignacionesQuery(Guid TesisId, int Etapa);

public sealed class ListarAsignacionesHandler(ITesisRepository tesis, IAsignacionRepository asignaciones, ICurrentUser actual)
    : IQueryHandler<ListarAsignacionesQuery, IReadOnlyList<AsignacionDto>>
{
    public async Task<IReadOnlyList<AsignacionDto>> HandleAsync(ListarAsignacionesQuery query, CancellationToken cancellationToken)
    {
        AutorizacionTesis.ExigirAdministrador(actual);

        var encontrada = await tesis.ObtenerPorIdAsync(query.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)query.Etapa);

        return (await asignaciones.ListarPorEtapaAsync(etapa.Id, cancellationToken)).Select(AsignacionDto.Desde).ToList();
    }
}
