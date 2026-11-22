namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>
/// Reglas de una etapa que el Product Owner puede cambiar sin tocar el dominio (ver ADR-005): se leen de la
/// configuración y el dominio solo las recibe como valor.
/// </summary>
/// <param name="PermiteReintento">
/// Si un rechazo permite corregir y reenviar la misma etapa (la tesis sigue en curso) o es definitivo (la tesis queda rechazada).
/// </param>
/// <param name="MaxEvaluadores">Tope de evaluadores activos asignados a la etapa.</param>
/// <param name="ExigeUnanimidad">Si aprobar exige un veredicto Favorable de todos los evaluadores activos en la ronda actual.</param>
public sealed record PoliticaEtapa(bool PermiteReintento, int MaxEvaluadores, bool ExigeUnanimidad)
{
    public const int MaxEvaluadoresPermitido = 10;

    /// <summary>
    /// Lo acordado con el Product Owner (2026-10-20): hasta 3 evaluadores, unanimidad, y rechazo reintentable salvo en
    /// Pasantía y Artículo, que son definitivas.
    /// </summary>
    public static PoliticaEtapa PorDefecto(EtapaTipo tipo) => new(
        PermiteReintento: tipo is not (EtapaTipo.Pasantia or EtapaTipo.Articulo),
        MaxEvaluadores: 3,
        ExigeUnanimidad: true);
}
