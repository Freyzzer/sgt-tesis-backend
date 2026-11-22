using System.Globalization;
using System.Text;
using FluentValidation;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;

namespace SGT.Tesis.Application.Reportes;

/// <summary>
/// Separador de columnas. Excel en configuración regional hispana espera <c>;</c> (y coma decimal); las demás
/// herramientas esperan coma (RFC 4180).
/// </summary>
public enum SeparadorCsv
{
    Coma = 1,
    PuntoYComa = 2,
}

public sealed record ArchivoCsv(byte[] Contenido, string NombreArchivo);

/// <summary>
/// Escritor CSV (RFC 4180) con UTF-8 y marca de orden de bytes para que Excel respete tildes y eñes.
/// Neutraliza la «inyección de fórmulas»: los títulos y nombres los escriben terceras personas y una celda que
/// empiece con <c>= + - @</c> se ejecutaría como fórmula al abrirla en una hoja de cálculo.
/// </summary>
public static class CsvEscritor
{
    private static readonly CultureInfo Hispana = CultureInfo.GetCultureInfo("es-CO");

    public static CultureInfo CulturaPara(SeparadorCsv separador) => separador == SeparadorCsv.PuntoYComa ? Hispana : CultureInfo.InvariantCulture;

    public static char Caracter(SeparadorCsv separador) => separador == SeparadorCsv.PuntoYComa ? ';' : ',';

    public static byte[] Escribir(IEnumerable<IReadOnlyList<string>> filas, SeparadorCsv separador)
    {
        var sep = Caracter(separador);
        var sb = new StringBuilder();

        foreach (var fila in filas)
        {
            for (var i = 0; i < fila.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(sep);
                }

                sb.Append(Celda(fila[i], sep));
            }

            sb.Append("\r\n");
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    public static string Celda(string? valor, char separador)
    {
        var texto = valor ?? string.Empty;

        if (texto.Length > 0 && texto[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            // Un número negativo legítimo no llega como texto: los valores numéricos se formatean aparte.
            texto = "'" + texto;
        }

        return texto.IndexOfAny([separador, '"', '\r', '\n']) >= 0
            ? "\"" + texto.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : texto;
    }
}

// ---------- Cola de validación ----------

public sealed record ExportarColaCsvQuery(SeparadorCsv Separador = SeparadorCsv.Coma);

public sealed class ExportarColaCsvQueryValidator : AbstractValidator<ExportarColaCsvQuery>
{
    public ExportarColaCsvQueryValidator() => RuleFor(x => x.Separador).IsInEnum();
}

/// <summary>Descarga completa de la cola de validación (hasta <see cref="MaximoDeFilas"/> filas).</summary>
public sealed class ExportarColaCsvHandler(
    IReportesRepository reportes,
    ICurrentUser actual,
    IClock clock,
    IAuditTrail audit,
    IUnitOfWork unitOfWork) : IQueryHandler<ExportarColaCsvQuery, ArchivoCsv>
{
    public const int MaximoDeFilas = 10_000;
    private const int TamanoDePagina = 500;

    public async Task<ArchivoCsv> HandleAsync(ExportarColaCsvQuery query, CancellationToken cancellationToken)
    {
        AdministradorRequerido.Exigir(actual);

        var ahora = clock.UtcNow;
        var cultura = CsvEscritor.CulturaPara(query.Separador);
        var filas = new List<IReadOnlyList<string>>
        {
            new[] { "Tesis", "Estudiante", "Etapa", "Tipo de etapa", "En revisión desde (UTC)", "Días en revisión", "Evaluadores asignados", "Evaluadores con veredicto" },
        };

        for (var pagina = 1; filas.Count - 1 < MaximoDeFilas; pagina++)
        {
            var (items, total) = await reportes.ListarColaAsync(ahora, pagina, TamanoDePagina, cancellationToken);

            foreach (var i in items.Take(MaximoDeFilas - (filas.Count - 1)))
            {
                filas.Add(new[]
                {
                    i.Titulo,
                    i.Estudiante,
                    i.Etapa.ToString(cultura),
                    i.TipoEtapa,
                    i.EnRevisionDesde.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    i.DiasEnRevision.ToString(cultura),
                    i.EvaluadoresAsignados.ToString(cultura),
                    i.EvaluadoresConVeredicto.ToString(cultura),
                });
            }

            if (items.Count == 0 || pagina * TamanoDePagina >= total)
            {
                break;
            }
        }

        audit.Registrar(AccionesAuditoria.ReporteExportado, "Reporte", detalle: $"Cola de validación: {filas.Count - 1} filas.");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ArchivoCsv(CsvEscritor.Escribir(filas, query.Separador), $"cola-validacion-{ahora:yyyyMMdd-HHmm}.csv");
    }
}

// ---------- KPIs ----------

public sealed record ExportarKpisCsvQuery(int DiasEstancamiento = 15, SeparadorCsv Separador = SeparadorCsv.Coma);

public sealed class ExportarKpisCsvQueryValidator : AbstractValidator<ExportarKpisCsvQuery>
{
    public ExportarKpisCsvQueryValidator()
    {
        RuleFor(x => x.DiasEstancamiento).InclusiveBetween(1, 365);
        RuleFor(x => x.Separador).IsInEnum();
    }
}

/// <summary>Los indicadores en formato largo (sección, concepto, métrica, valor), listo para una tabla dinámica.</summary>
public sealed class ExportarKpisCsvHandler(
    ObtenerKpisHandler kpis,
    ICurrentUser actual,
    IAuditTrail audit,
    IUnitOfWork unitOfWork) : IQueryHandler<ExportarKpisCsvQuery, ArchivoCsv>
{
    public async Task<ArchivoCsv> HandleAsync(ExportarKpisCsvQuery query, CancellationToken cancellationToken)
    {
        AdministradorRequerido.Exigir(actual);

        var k = await kpis.HandleAsync(new ObtenerKpisQuery(query.DiasEstancamiento), cancellationToken);
        var c = CsvEscritor.CulturaPara(query.Separador);
        string N(int v) => v.ToString(c);
        string D(double v) => v.ToString("0.##", c);

        var filas = new List<IReadOnlyList<string>> { new[] { "Sección", "Concepto", "Métrica", "Valor" } };

        void Fila(string seccion, string concepto, string metrica, string valor) => filas.Add([seccion, concepto, metrica, valor]);

        Fila("Resumen", "Generado en (UTC)", "Fecha", k.GeneradoEn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        Fila("Resumen", "Todas", "Tesis registradas", N(k.TotalTesis));
        Fila("Resumen", "Todas", "Etapas en revisión", N(k.EtapasEnRevision));
        Fila("Resumen", $"Más de {k.DiasEstancamiento} días", "Etapas estancadas", N(k.EtapasEstancadas));

        foreach (var (estado, cantidad) in k.TesisPorEstado)
        {
            Fila("Tesis por estado", estado, "Tesis", N(cantidad));
        }

        foreach (var e in k.TesisEnCursoPorEtapa)
        {
            Fila("Tesis en curso por etapa", $"{e.Numero}. {e.Tipo}", "Tesis", N(e.Cantidad));
        }

        foreach (var t in k.TiempoPromedioPorEtapa)
        {
            Fila("Tiempo por etapa", $"{t.Numero}. {t.Tipo}", "Etapas cerradas", N(t.EtapasCerradas));
            Fila("Tiempo por etapa", $"{t.Numero}. {t.Tipo}", "Promedio (días)", D(t.PromedioDias));
        }

        Fila("Decisiones", "Todas", "Total", N(k.Decisiones.Total));
        Fila("Decisiones", "Aprobadas", "Cantidad", N(k.Decisiones.Aprobadas));
        Fila("Decisiones", "Observadas", "Cantidad", N(k.Decisiones.Observadas));
        Fila("Decisiones", "Rechazadas", "Cantidad", N(k.Decisiones.Rechazadas));
        Fila("Decisiones", "Aprobadas", "Tasa (%)", D(k.Decisiones.TasaAprobacion * 100));
        Fila("Decisiones", "Observadas", "Tasa (%)", D(k.Decisiones.TasaObservacion * 100));
        Fila("Decisiones", "Rechazadas", "Tasa (%)", D(k.Decisiones.TasaRechazo * 100));

        foreach (var ev in k.CargaPorEvaluador)
        {
            Fila("Carga por evaluador", ev.Nombre, "Asignaciones activas", N(ev.AsignacionesActivas));
            Fila("Carga por evaluador", ev.Nombre, "Veredictos pendientes", N(ev.VeredictosPendientes));
        }

        audit.Registrar(AccionesAuditoria.ReporteExportado, "Reporte", detalle: $"Indicadores (estancamiento {k.DiasEstancamiento} días).");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ArchivoCsv(CsvEscritor.Escribir(filas, query.Separador), $"indicadores-{k.GeneradoEn:yyyyMMdd-HHmm}.csv");
    }
}
