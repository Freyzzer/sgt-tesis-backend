using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Evaluacion;

/// <summary>Reglas de lectura de los veredictos por ronda (ADR-005).</summary>
public static class CalculoDeEvaluacion
{
    /// <summary>
    /// El veredicto vigente de cada evaluador en la ronda indicada: el de mayor versión emitido en esa ronda.
    /// Los de rondas anteriores se conservan como historial pero no cuentan.
    /// </summary>
    public static IReadOnlyDictionary<Guid, Veredicto> VigentesDeLaRonda(IEnumerable<Veredicto> veredictos, int ronda) =>
        veredictos
            .Where(v => v.Ronda == ronda)
            .GroupBy(v => v.EvaluadorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.Version).First());

    /// <summary>Evaluadores activos de la etapa y cuántos tienen un veredicto Favorable vigente en la ronda actual.</summary>
    public static ResumenEvaluacion Resumir(Etapa etapa, IEnumerable<AsignacionEvaluador> asignaciones, IEnumerable<Veredicto> veredictos)
    {
        var activos = asignaciones.Where(a => a.Activa).Select(a => a.EvaluadorId).Distinct().ToList();
        var vigentes = VigentesDeLaRonda(veredictos, etapa.Ronda);
        var favorables = activos.Count(id => vigentes.TryGetValue(id, out var v) && v.Concepto == ConceptoVeredicto.Favorable);

        return new ResumenEvaluacion(activos.Count, favorables);
    }
}
