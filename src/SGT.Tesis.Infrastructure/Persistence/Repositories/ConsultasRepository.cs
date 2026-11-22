using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Consultas;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

/// <summary>Consultas de lectura para listados (proyecciones, sin cargar entidades completas).</summary>
internal sealed class ConsultasRepository(AppDbContext db) : IConsultasRepository
{
    public async Task<(IReadOnlyList<EvaluacionAsignadaDto> Items, int Total)> ListarEvaluacionesAsignadasAsync(
        Guid evaluadorId,
        bool soloPendientes,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var consulta =
            from a in db.Asignaciones.AsNoTracking()
            where a.EvaluadorId == evaluadorId && a.Activa
            join e in db.Etapas.AsNoTracking() on a.EtapaId equals e.Id
            join t in db.Tesis.AsNoTracking() on e.TesisId equals t.Id
            join u in db.Usuarios.AsNoTracking() on t.StudentId equals u.Id
            select new
            {
                TesisId = t.Id,
                t.Titulo,
                u.Nombres,
                u.Apellidos,
                e.Tipo,
                EstadoEtapa = e.Estado,
                a.FechaAsignacion,
                e.EnRevisionDesde,
                VeredictoEmitido = db.Veredictos.Any(v => v.EtapaId == e.Id && v.EvaluadorId == evaluadorId),
            };

        if (soloPendientes)
        {
            // Pendiente = la etapa está en revisión y todavía no emitió veredicto.
            consulta = consulta.Where(x => x.EstadoEtapa == EstadoEtapa.EnRevision && !x.VeredictoEmitido);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var filas = await consulta
            .OrderByDescending(x => x.EstadoEtapa == EstadoEtapa.EnRevision)
            .ThenBy(x => x.EnRevisionDesde ?? x.FechaAsignacion)
            .ThenBy(x => x.TesisId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (
            filas.Select(f => new EvaluacionAsignadaDto(
                f.TesisId,
                f.Titulo,
                $"{f.Nombres} {f.Apellidos}",
                (int)f.Tipo,
                f.Tipo.ToString(),
                f.EstadoEtapa.ToString(),
                f.FechaAsignacion,
                f.EnRevisionDesde,
                f.VeredictoEmitido)).ToList(),
            total);
    }

    public async Task<(IReadOnlyList<TesisResumenDto> Items, int Total)> ListarTesisAsync(
        ListarTesisQuery filtro,
        CancellationToken cancellationToken)
    {
        var consulta =
            from t in db.Tesis.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on t.StudentId equals u.Id
            let actual = t.Etapas.OrderByDescending(e => e.Tipo).FirstOrDefault()
            select new { Tesis = t, Usuario = u, Actual = actual };

        if (filtro.Estado is { } estado)
        {
            consulta = consulta.Where(x => x.Tesis.Estado == estado);
        }

        if (filtro.Etapa is { } etapa)
        {
            var tipo = (EtapaTipo)etapa;
            consulta = consulta.Where(x => x.Actual != null && x.Actual.Tipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(x =>
                x.Tesis.Titulo.Contains(texto) || x.Usuario.Nombres.Contains(texto) || x.Usuario.Apellidos.Contains(texto) || x.Usuario.Correo.Contains(texto));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var filas = await consulta
            .OrderByDescending(x => x.Tesis.CreatedAt).ThenBy(x => x.Tesis.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => new
            {
                x.Tesis.Id,
                x.Tesis.Titulo,
                EstudianteId = x.Usuario.Id,
                x.Usuario.Nombres,
                x.Usuario.Apellidos,
                EstadoTesis = x.Tesis.Estado,
                Tipo = x.Actual!.Tipo,
                EstadoEtapa = x.Actual.Estado,
                x.Tesis.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return (
            filas.Select(f => new TesisResumenDto(
                f.Id,
                f.Titulo,
                f.EstudianteId,
                $"{f.Nombres} {f.Apellidos}",
                f.EstadoTesis.ToString(),
                (int)f.Tipo,
                f.Tipo.ToString(),
                f.EstadoEtapa.ToString(),
                f.CreatedAt)).ToList(),
            total);
    }
}
