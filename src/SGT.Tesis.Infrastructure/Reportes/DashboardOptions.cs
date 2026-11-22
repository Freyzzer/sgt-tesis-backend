using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Reportes;

namespace SGT.Tesis.Infrastructure.Reportes;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    /// <summary>Segundos que se conservan los KPIs en memoria (0 = sin caché; máximo 600).</summary>
    public int KpisCacheSegundos { get; set; } = 30;
}

internal sealed class DashboardPolicy(IOptions<DashboardOptions> options) : IReportesPolicy
{
    public TimeSpan KpisCacheTtl => TimeSpan.FromSeconds(Math.Clamp(options.Value.KpisCacheSegundos, 0, 600));
}
