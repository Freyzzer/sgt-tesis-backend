using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

internal sealed class AsignacionRepository(AppDbContext db) : IAsignacionRepository
{
    public Task<AsignacionEvaluador?> ObtenerActivaAsync(Guid etapaId, Guid evaluadorId, CancellationToken cancellationToken) =>
        db.Asignaciones.FirstOrDefaultAsync(a => a.EtapaId == etapaId && a.EvaluadorId == evaluadorId && a.Activa, cancellationToken);

    public async Task<IReadOnlyList<AsignacionEvaluador>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
        await db.Asignaciones.AsNoTracking()
            .Where(a => a.EtapaId == etapaId)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToListAsync(cancellationToken);

    public Task<bool> TieneAsignacionActivaEnTesisAsync(Guid evaluadorId, Guid tesisId, CancellationToken cancellationToken) =>
        db.Asignaciones.AnyAsync(
            a => a.EvaluadorId == evaluadorId && a.Activa
                && db.Tesis.Any(t => t.Id == tesisId && t.Etapas.Any(e => e.Id == a.EtapaId)),
            cancellationToken);

    public void Agregar(AsignacionEvaluador asignacion) => db.Asignaciones.Add(asignacion);
}

internal sealed class VeredictoRepository(AppDbContext db) : IVeredictoRepository
{
    public async Task<int> UltimaVersionAsync(Guid etapaId, Guid evaluadorId, CancellationToken cancellationToken) =>
        await db.Veredictos
            .Where(v => v.EtapaId == etapaId && v.EvaluadorId == evaluadorId)
            .Select(v => (int?)v.Version)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task<IReadOnlyList<Veredicto>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
        await db.Veredictos.AsNoTracking().Where(v => v.EtapaId == etapaId).ToListAsync(cancellationToken);

    public void Agregar(Veredicto veredicto) => db.Veredictos.Add(veredicto);
}

internal sealed class DecisionRepository(AppDbContext db) : IDecisionRepository
{
    public async Task<IReadOnlyList<Decision>> ListarPorTesisAsync(Guid tesisId, CancellationToken cancellationToken) =>
        await db.Decisiones.AsNoTracking()
            .Where(d => db.Tesis.Any(t => t.Id == tesisId && t.Etapas.Any(e => e.Id == d.EtapaId)))
            .OrderBy(d => d.FechaUtc)
            .ToListAsync(cancellationToken);

    public void Agregar(Decision decision) => db.Decisiones.Add(decision);
}
