using Microsoft.Extensions.Options;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Flujo;

/// <summary>
/// Reglas del flujo por etapa (sección <c>Flujo</c>). Cada valor es opcional: lo que no se indica usa el valor
/// acordado por defecto (<see cref="PoliticaEtapa.PorDefecto"/>). Ver <c>docs/PROCESO_DE_ETAPAS.md</c>.
/// </summary>
public sealed class FlujoOptions
{
    public const string SectionName = "Flujo";

    /// <summary>Clave: nombre de la etapa (Estudiante, Propuesta, Consejo, Evaluacion, Pasantia, Articulo o RequisitosFinales).</summary>
    public Dictionary<string, PoliticaEtapaOptions> Etapas { get; set; } = [];
}

public sealed class PoliticaEtapaOptions
{
    public bool? PermiteReintento { get; set; }

    public int? MaxEvaluadores { get; set; }

    public bool? ExigeUnanimidad { get; set; }
}

internal sealed class FlujoOptionsValidator : IValidateOptions<FlujoOptions>
{
    public ValidateOptionsResult Validate(string? name, FlujoOptions options)
    {
        var errors = new List<string>();

        foreach (var (clave, politica) in options.Etapas)
        {
            if (!Enum.TryParse<EtapaTipo>(clave, out _))
            {
                errors.Add($"Flujo:Etapas:{clave} no es una etapa válida ({string.Join(", ", Enum.GetNames<EtapaTipo>())}).");
            }

            if (politica.MaxEvaluadores is < 1 or > PoliticaEtapa.MaxEvaluadoresPermitido)
            {
                errors.Add($"Flujo:Etapas:{clave}:MaxEvaluadores debe estar entre 1 y {PoliticaEtapa.MaxEvaluadoresPermitido}.");
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

/// <summary>Combina la configuración con los valores acordados por defecto.</summary>
internal sealed class PoliticasDeEtapaConfigurables(IOptions<FlujoOptions> options) : IPoliticasDeEtapa
{
    public PoliticaEtapa Para(EtapaTipo tipo)
    {
        var porDefecto = PoliticaEtapa.PorDefecto(tipo);

        if (!options.Value.Etapas.TryGetValue(tipo.ToString(), out var configurada))
        {
            return porDefecto;
        }

        return new PoliticaEtapa(
            configurada.PermiteReintento ?? porDefecto.PermiteReintento,
            configurada.MaxEvaluadores ?? porDefecto.MaxEvaluadores,
            configurada.ExigeUnanimidad ?? porDefecto.ExigeUnanimidad);
    }
}
