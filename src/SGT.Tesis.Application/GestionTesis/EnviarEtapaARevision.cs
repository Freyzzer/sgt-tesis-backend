using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
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
    IRequisitosDocumentales requisitos,
    ICurrentUser actual,
    IUnitOfWork unitOfWork,
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

        propia.EnviarEtapaARevision(tipo);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Etapa {Etapa} de la tesis {TesisId} enviada a revisión por {ActorId}", tipo, propia.Id, actual.UserId);

        return TesisDto.Desde(propia, await decisiones.ListarPorTesisAsync(propia.Id, cancellationToken));
    }
}
