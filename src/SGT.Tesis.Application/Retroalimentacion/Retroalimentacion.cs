using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;

namespace SGT.Tesis.Application.Retroalimentacion;

/// <summary>Veredicto vigente sin identificar al evaluador (la evaluación es anónima para el estudiante).</summary>
public sealed record VeredictoAnonimoDto(string Evaluador, string Concepto, string Comentarios, DateTime ActualizadoEn);

public sealed record EtapaRetroalimentacionDto(
    int Numero,
    string Tipo,
    string Estado,
    IReadOnlyList<DecisionDto> Decisiones,
    IReadOnlyList<VeredictoAnonimoDto> Veredictos);

public sealed record RetroalimentacionDto(Guid TesisId, IReadOnlyList<EtapaRetroalimentacionDto> Etapas);

public sealed record ObtenerRetroalimentacionQuery(Guid TesisId);

/// <summary>
/// Retroalimentación consolidada por etapa: las decisiones del Administrador con su justificación y, una vez
/// decidida la etapa, el veredicto vigente de cada evaluador de forma anónima ("Evaluador 1", "Evaluador 2"…).
/// Mientras la etapa no tenga decisión no se muestran veredictos: son conceptos previos a la decisión.
/// </summary>
public sealed class ObtenerRetroalimentacionHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IVeredictoRepository veredictos,
    ICurrentUser actual) : IQueryHandler<ObtenerRetroalimentacionQuery, RetroalimentacionDto>
{
    public async Task<RetroalimentacionDto> HandleAsync(ObtenerRetroalimentacionQuery query, CancellationToken cancellationToken)
    {
        var encontrada = await tesis.ObtenerPorIdAsync(query.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        // Solo el dueño y el Administrador; para el resto la tesis "no existe".
        AutorizacionTesis.ExigirVisible(encontrada, actual);

        var todas = await decisiones.ListarPorTesisAsync(encontrada.Id, cancellationToken);
        var etapas = new List<EtapaRetroalimentacionDto>();

        foreach (var etapa in encontrada.Etapas.OrderBy(e => e.Tipo))
        {
            var deLaEtapa = todas.Where(d => d.EtapaId == etapa.Id).ToList();
            var anonimos = new List<VeredictoAnonimoDto>();

            if (deLaEtapa.Count > 0)
            {
                var porEvaluador = (await veredictos.ListarPorEtapaAsync(etapa.Id, cancellationToken))
                    .GroupBy(v => v.EvaluadorId)
                    .Select(g => new { Primero = g.Min(v => v.CreatedAt), Id = g.Key, Vigente = g.OrderByDescending(v => v.Version).First() })
                    .OrderBy(x => x.Primero).ThenBy(x => x.Id)
                    .ToList();

                for (var i = 0; i < porEvaluador.Count; i++)
                {
                    var vigente = porEvaluador[i].Vigente;
                    anonimos.Add(new VeredictoAnonimoDto($"Evaluador {i + 1}", vigente.Concepto.ToString(), vigente.Comentarios, vigente.CreatedAt));
                }
            }

            etapas.Add(new EtapaRetroalimentacionDto(
                (int)etapa.Tipo,
                etapa.Tipo.ToString(),
                etapa.Estado.ToString(),
                deLaEtapa.OrderBy(d => d.FechaUtc).Select(d => DecisionDto.Desde(d, encontrada)).ToList(),
                anonimos));
        }

        return new RetroalimentacionDto(encontrada.Id, etapas);
    }
}
