namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>
/// Cómo van los evaluadores de una etapa en su ronda actual: cuántos hay activos y cuántos ya dieron un veredicto
/// <em>Favorable</em> vigente. Lo calcula la capa de aplicación; el dominio solo aplica la regla de unanimidad (ADR-005).
/// </summary>
public sealed record ResumenEvaluacion(int Activos, int Favorables)
{
    public bool HayEvaluadores => Activos > 0;

    public bool EsUnanime => Favorables >= Activos;

    public int Pendientes => Math.Max(0, Activos - Favorables);
}
