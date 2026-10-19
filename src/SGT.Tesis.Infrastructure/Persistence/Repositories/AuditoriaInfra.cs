using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Auditoria;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

internal sealed class AuditTrail(AppDbContext db, ICurrentUser actual, IRequestContext solicitud, IClock clock) : IAuditTrail
{
    private DateTime _ultima = DateTime.MinValue;

    public void Registrar(string accion, string entidad, Guid? entidadId = null, Guid? tesisId = null, string? detalle = null, Guid? actorId = null)
    {
        // Los eventos de una misma solicitud llevan fechas estrictamente crecientes: así el orden cronológico no
        // depende del desempate por Id (que en SQL Server no sigue el orden de creación).
        var ahora = clock.UtcNow;

        if (ahora <= _ultima)
        {
            ahora = _ultima.AddTicks(1);
        }

        _ultima = ahora;

        db.RegistrosAuditoria.Add(RegistroAuditoria.Crear(
            accion,
            entidad,
            actorId ?? actual.UserId,
            entidadId,
            tesisId,
            detalle,
            solicitud.Ip,
            solicitud.CorrelationId,
            ahora));
    }
}

internal sealed class AuditoriaRepository(AppDbContext db) : IAuditoriaRepository
{
    public async Task<(IReadOnlyList<RegistroAuditoria> Items, int Total)> ListarAsync(
        ConsultarAuditoriaQuery filtro,
        CancellationToken cancellationToken)
    {
        var consulta = db.RegistrosAuditoria.AsNoTracking().AsQueryable();

        if (filtro.TesisId is { } tesisId)
        {
            consulta = consulta.Where(r => r.TesisId == tesisId);
        }

        if (filtro.ActorId is { } actorId)
        {
            consulta = consulta.Where(r => r.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            var accion = filtro.Accion.Trim();
            consulta = consulta.Where(r => r.Accion.StartsWith(accion));
        }

        if (filtro.Desde is { } desde)
        {
            consulta = consulta.Where(r => r.FechaUtc >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            consulta = consulta.Where(r => r.FechaUtc <= hasta);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var items = await consulta
            .OrderByDescending(r => r.FechaUtc).ThenByDescending(r => r.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
