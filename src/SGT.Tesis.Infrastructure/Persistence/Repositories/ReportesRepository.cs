using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Reportes;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas de lectura con proyecciones (sin cargar entidades completas) para el panel del Administrador.
/// Las cuentas se calculan en la base de datos; solo el promedio de días por etapa se calcula en memoria
/// sobre dos fechas por etapa cerrada, porque la resta de fechas no es portable entre proveedores.
/// </summary>
internal sealed class ReportesRepository(AppDbContext db) : IReportesRepository
{
    public async Task<KpisDto> ObtenerKpisAsync(DateTime ahora, int diasEstancamiento, CancellationToken cancellationToken)
    {
        var porEstado = await db.Tesis.AsNoTracking()
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancellationToken);

        // Etapa actual de cada tesis en curso = la de mayor número abierta; como solo hay una abierta, basta contar las etapas abiertas.
        var enCursoPorEtapa = await db.Tesis.AsNoTracking()
            .Where(t => t.Estado == EstadoTesis.EnCurso)
            .SelectMany(t => t.Etapas)
            .Where(e => e.Estado == EstadoEtapa.Pendiente || e.Estado == EstadoEtapa.EnRevision || e.Estado == EstadoEtapa.Observado)
            .GroupBy(e => e.Tipo)
            .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancellationToken);

        var cerradas = await db.Etapas.AsNoTracking()
            .Where(e => e.FechaCierre != null)
            .Select(e => new { e.Tipo, e.FechaApertura, FechaCierre = e.FechaCierre!.Value })
            .ToListAsync(cancellationToken);

        var decisiones = await db.Decisiones.AsNoTracking()
            .GroupBy(d => d.Resultado)
            .Select(g => new { Resultado = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancellationToken);

        var limite = ahora.AddDays(-diasEstancamiento);
        var enRevision = await db.Etapas.AsNoTracking().CountAsync(e => e.Estado == EstadoEtapa.EnRevision, cancellationToken);
        var estancadas = await db.Etapas.AsNoTracking()
            .CountAsync(e => e.Estado == EstadoEtapa.EnRevision && (e.EnRevisionDesde ?? e.FechaApertura) <= limite, cancellationToken);

        var carga = await (
            from a in db.Asignaciones.AsNoTracking()
            where a.Activa
            join e in db.Etapas.AsNoTracking() on a.EtapaId equals e.Id
            where e.Estado == EstadoEtapa.EnRevision
            join u in db.Usuarios.AsNoTracking() on a.EvaluadorId equals u.Id
            select new
            {
                a.EvaluadorId,
                u.Nombres,
                u.Apellidos,
                TieneVeredicto = db.Veredictos.Any(v => v.EtapaId == a.EtapaId && v.EvaluadorId == a.EvaluadorId),
            }).ToListAsync(cancellationToken);

        int Cantidad(ResultadoDecision resultado) => decisiones.FirstOrDefault(d => d.Resultado == resultado)?.Cantidad ?? 0;

        var aprobadas = Cantidad(ResultadoDecision.Aprobada);
        var observadas = Cantidad(ResultadoDecision.Observada);
        var rechazadas = Cantidad(ResultadoDecision.Rechazada);
        var total = aprobadas + observadas + rechazadas;

        double Tasa(int parte) => total == 0 ? 0 : Math.Round(parte * 100.0 / total, 1);

        return new KpisDto(
            ahora,
            porEstado.Sum(x => x.Cantidad),
            Enum.GetValues<EstadoTesis>().ToDictionary(e => e.ToString(), e => porEstado.FirstOrDefault(x => x.Estado == e)?.Cantidad ?? 0),
            Enum.GetValues<EtapaTipo>()
                .Select(t => new TesisPorEtapaDto((int)t, t.ToString(), enCursoPorEtapa.FirstOrDefault(x => x.Tipo == t)?.Cantidad ?? 0))
                .ToList(),
            Enum.GetValues<EtapaTipo>()
                .Select(t =>
                {
                    var delTipo = cerradas.Where(c => c.Tipo == t).ToList();
                    var promedio = delTipo.Count == 0 ? 0 : Math.Round(delTipo.Average(c => (c.FechaCierre - c.FechaApertura).TotalDays), 1);

                    return new TiempoPorEtapaDto((int)t, t.ToString(), delTipo.Count, promedio);
                })
                .ToList(),
            new DecisionesKpiDto(total, aprobadas, observadas, rechazadas, Tasa(aprobadas), Tasa(observadas), Tasa(rechazadas)),
            enRevision,
            diasEstancamiento,
            estancadas,
            carga.GroupBy(c => c.EvaluadorId)
                .Select(g => new CargaEvaluadorDto(
                    g.Key,
                    $"{g.First().Nombres} {g.First().Apellidos}",
                    g.Count(),
                    g.Count(x => !x.TieneVeredicto)))
                .OrderByDescending(c => c.VeredictosPendientes).ThenBy(c => c.Nombre)
                .ToList());
    }

    public async Task<(IReadOnlyList<ItemColaValidacionDto> Items, int Total)> ListarColaAsync(
        DateTime ahora,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var enRevision = db.Etapas.AsNoTracking().Where(e => e.Estado == EstadoEtapa.EnRevision);
        var total = await enRevision.CountAsync(cancellationToken);

        var filas = await (
            from e in enRevision
            join t in db.Tesis.AsNoTracking() on e.TesisId equals t.Id
            join u in db.Usuarios.AsNoTracking() on t.StudentId equals u.Id
            orderby e.EnRevisionDesde ?? e.FechaApertura, e.Id
            select new
            {
                TesisId = t.Id,
                EstudianteId = u.Id,
                u.Nombres,
                u.Apellidos,
                t.Titulo,
                e.Tipo,
                Desde = e.EnRevisionDesde ?? e.FechaApertura,
                Asignados = db.Asignaciones.Count(a => a.EtapaId == e.Id && a.Activa),
                ConVeredicto = db.Asignaciones.Count(a => a.EtapaId == e.Id && a.Activa
                    && db.Veredictos.Any(v => v.EtapaId == e.Id && v.EvaluadorId == a.EvaluadorId)),
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = filas.Select(f => new ItemColaValidacionDto(
                f.TesisId,
                f.EstudianteId,
                $"{f.Nombres} {f.Apellidos}",
                f.Titulo,
                (int)f.Tipo,
                f.Tipo.ToString(),
                f.Desde,
                Math.Max(0, (int)(ahora - f.Desde).TotalDays),
                f.Asignados,
                f.ConVeredicto))
            .ToList();

        return (items, total);
    }
}
