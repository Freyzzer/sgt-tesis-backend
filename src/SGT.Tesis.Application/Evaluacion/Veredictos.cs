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

public sealed record EmitirVeredictoCommand(Guid TesisId, int Etapa, ConceptoVeredicto Concepto, string Comentarios);

public sealed class EmitirVeredictoCommandValidator : AbstractValidator<EmitirVeredictoCommand>
{
    public EmitirVeredictoCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.Concepto).IsInEnum();
        RuleFor(x => x.Comentarios).NotEmpty().MaximumLength(Veredicto.ComentariosMaxLength);
    }
}

/// <summary>
/// El veredicto es un concepto técnico no vinculante: nunca modifica el estado de la etapa ni de la tesis (INV-5).
/// Cada emisión crea una versión nueva; las anteriores quedan como historial.
/// </summary>
public sealed class EmitirVeredictoHandler(
    ITesisRepository tesis,
    IAsignacionRepository asignaciones,
    IVeredictoRepository veredictos,
    IUsuarioRepository usuarios,
    INotificador notificador,
    ICurrentUser actual,
    IUnitOfWork unitOfWork,
    IAuditTrail audit,
    ILogger<EmitirVeredictoHandler> logger) : ICommandHandler<EmitirVeredictoCommand, VeredictoDto>
{
    public async Task<VeredictoDto> HandleAsync(EmitirVeredictoCommand command, CancellationToken cancellationToken)
    {
        if (actual.UserId is not { } evaluadorId || actual.Rol != nameof(Rol.Evaluador))
        {
            throw new ProhibidoException("Solo un Evaluador puede emitir veredictos.");
        }

        var encontrada = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)command.Etapa);

        if (await asignaciones.ObtenerActivaAsync(etapa.Id, evaluadorId, cancellationToken) is null)
        {
            throw new ProhibidoException("No tiene una asignación activa en esta etapa.");
        }

        if (etapa.Estado != EstadoEtapa.EnRevision)
        {
            throw new TransicionInvalidaException("Solo se pueden emitir veredictos mientras la etapa está en revisión.");
        }

        var version = await veredictos.UltimaVersionAsync(etapa.Id, evaluadorId, cancellationToken) + 1;
        var veredicto = Veredicto.Crear(etapa.Id, evaluadorId, command.Concepto, command.Comentarios, version, etapa.Ronda);
        veredictos.Agregar(veredicto);

        // El estudiante ve su proceso actualizado: se le avisa de cada evaluación (sin concepto ni comentarios en el correo).
        if (await usuarios.ObtenerPorIdAsync(encontrada.StudentId, cancellationToken) is { } estudiante)
        {
            var (asunto, cuerpo) = Plantillas.VeredictoEmitido(etapa.Tipo, etapa.Ronda);
            notificador.Encolar(estudiante.Correo, asunto, cuerpo);
        }

        audit.Registrar(AccionesAuditoria.VeredictoEmitido, "Veredicto", veredicto.Id, encontrada.Id, $"{command.Concepto}, etapa {etapa.Tipo}, v{version}.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Veredicto v{Version} ({Concepto}) del evaluador {EvaluadorId} sobre la etapa {Etapa} de la tesis {TesisId}", version, veredicto.Concepto, evaluadorId, etapa.Tipo, encontrada.Id);

        return VeredictoDto.Desde(veredicto);
    }
}

public sealed record ListarVeredictosQuery(Guid TesisId, int Etapa, bool SoloVigentes = false);

public sealed class ListarVeredictosHandler(ITesisRepository tesis, IVeredictoRepository veredictos, ICurrentUser actual)
    : IQueryHandler<ListarVeredictosQuery, IReadOnlyList<VeredictoDto>>
{
    public async Task<IReadOnlyList<VeredictoDto>> HandleAsync(ListarVeredictosQuery query, CancellationToken cancellationToken)
    {
        var esAdministrador = actual.Rol == nameof(Rol.Administrador);
        var esEvaluador = actual.Rol == nameof(Rol.Evaluador) && actual.UserId is not null;

        if (!esAdministrador && !esEvaluador)
        {
            throw new ProhibidoException();
        }

        var encontrada = await tesis.ObtenerPorIdAsync(query.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        var etapa = encontrada.ObtenerEtapa((EtapaTipo)query.Etapa);
        IEnumerable<Veredicto> lista = await veredictos.ListarPorEtapaAsync(etapa.Id, cancellationToken);

        // Un Evaluador solo ve sus propios veredictos; el Administrador ve todos.
        if (!esAdministrador)
        {
            lista = lista.Where(v => v.EvaluadorId == actual.UserId);
        }

        // Vigentes = el último veredicto de cada evaluador en la ronda actual.
        if (query.SoloVigentes)
        {
            lista = CalculoDeEvaluacion.VigentesDeLaRonda(lista, etapa.Ronda).Values;
        }

        return lista.OrderBy(v => v.EvaluadorId).ThenByDescending(v => v.Version).Select(VeredictoDto.Desde).ToList();
    }
}
