using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;

namespace SGT.Tesis.Application.Retroalimentacion;

/// <summary>
/// Veredicto vigente de un evaluador en una ronda. Por defecto el evaluador es anónimo («Evaluador 1»…); con
/// <c>Flujo:MostrarNombreEvaluador</c> se muestra su nombre.
/// </summary>
public sealed record VeredictoVisibleDto(string Evaluador, string Concepto, string Comentarios, int Ronda, DateTime ActualizadoEn);

public sealed record EtapaRetroalimentacionDto(
    int Numero,
    string Tipo,
    string Estado,
    int Ronda,
    IReadOnlyList<DecisionDto> Decisiones,
    IReadOnlyList<VeredictoVisibleDto> Veredictos);

public sealed record RetroalimentacionDto(Guid TesisId, IReadOnlyList<EtapaRetroalimentacionDto> Etapas);

public sealed record ObtenerRetroalimentacionQuery(Guid TesisId);

/// <summary>
/// Retroalimentación consolidada por etapa: las decisiones del Administrador con su justificación y el veredicto
/// vigente de cada evaluador en cada ronda, apenas se emite (el estudiante ve su proceso actualizado; ADR-005).
/// Los veredictos reemplazados dentro de una ronda no se muestran, solo el último de cada evaluador.
/// </summary>
public sealed class ObtenerRetroalimentacionHandler(
    ITesisRepository tesis,
    IDecisionRepository decisiones,
    IVeredictoRepository veredictos,
    IUsuarioRepository usuarios,
    IPoliticasDeEtapa politicas,
    ICurrentUser actual) : IQueryHandler<ObtenerRetroalimentacionQuery, RetroalimentacionDto>
{
    public async Task<RetroalimentacionDto> HandleAsync(ObtenerRetroalimentacionQuery query, CancellationToken cancellationToken)
    {
        var encontrada = await tesis.ObtenerPorIdAsync(query.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        // Solo el dueño y el Administrador; para el resto la tesis "no existe".
        AutorizacionTesis.ExigirVisible(encontrada, actual);

        var todas = await decisiones.ListarPorTesisAsync(encontrada.Id, cancellationToken);
        var mostrarNombre = politicas.MostrarNombreEvaluador;
        var nombres = new Dictionary<Guid, string>();
        var etapas = new List<EtapaRetroalimentacionDto>();

        foreach (var etapa in encontrada.Etapas.OrderBy(e => e.Tipo))
        {
            var deLaEtapa = todas.Where(d => d.EtapaId == etapa.Id).ToList();
            var visibles = new List<VeredictoVisibleDto>();
            var emitidos = await veredictos.ListarPorEtapaAsync(etapa.Id, cancellationToken);

            if (emitidos.Count > 0)
            {
                // «Evaluador N» sigue el orden en que cada evaluador emitió por primera vez y se conserva entre rondas.
                var orden = emitidos
                    .GroupBy(v => v.EvaluadorId)
                    .Select(g => new { Id = g.Key, Primero = g.Min(v => v.CreatedAt) })
                    .OrderBy(x => x.Primero).ThenBy(x => x.Id)
                    .Select((x, i) => (x.Id, Numero: i + 1))
                    .ToDictionary(x => x.Id, x => x.Numero);

                foreach (var ronda in emitidos.Select(v => v.Ronda).Distinct().Order())
                {
                    foreach (var vigente in CalculoDeEvaluacion.VigentesDeLaRonda(emitidos, ronda).Values.OrderBy(v => orden[v.EvaluadorId]))
                    {
                        var quien = $"Evaluador {orden[vigente.EvaluadorId]}";

                        if (mostrarNombre)
                        {
                            if (!nombres.TryGetValue(vigente.EvaluadorId, out var nombre))
                            {
                                var usuario = await usuarios.ObtenerPorIdAsync(vigente.EvaluadorId, cancellationToken);
                                nombre = usuario is null ? quien : $"{usuario.Nombres} {usuario.Apellidos}";
                                nombres[vigente.EvaluadorId] = nombre;
                            }

                            quien = nombre;
                        }

                        visibles.Add(new VeredictoVisibleDto(quien, vigente.Concepto.ToString(), vigente.Comentarios, vigente.Ronda, vigente.CreatedAt));
                    }
                }
            }

            etapas.Add(new EtapaRetroalimentacionDto(
                (int)etapa.Tipo,
                etapa.Tipo.ToString(),
                etapa.Estado.ToString(),
                etapa.Ronda,
                deLaEtapa.OrderBy(d => d.FechaUtc).Select(d => DecisionDto.Desde(d, encontrada)).ToList(),
                visibles));
        }

        return new RetroalimentacionDto(encontrada.Id, etapas);
    }
}
