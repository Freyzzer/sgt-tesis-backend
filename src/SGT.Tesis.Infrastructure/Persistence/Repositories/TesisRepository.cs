using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

internal sealed class TesisRepository(AppDbContext db) : ITesisRepository
{
    public Task<TesisGrado?> ObtenerActivaDeEstudianteAsync(Guid studentId, CancellationToken cancellationToken) =>
        db.Tesis.Include(t => t.Etapas)
            .FirstOrDefaultAsync(t => t.StudentId == studentId && t.Estado == EstadoTesis.EnCurso, cancellationToken);

    public Task<TesisGrado?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Tesis.Include(t => t.Etapas).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TesisGrado?> ObtenerPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
        db.Tesis.Include(t => t.Etapas).FirstOrDefaultAsync(t => t.Etapas.Any(e => e.Id == etapaId), cancellationToken);

    public Task<bool> ExisteActivaAsync(Guid studentId, CancellationToken cancellationToken) =>
        db.Tesis.AnyAsync(t => t.StudentId == studentId && t.Estado == EstadoTesis.EnCurso, cancellationToken);

    public void Agregar(TesisGrado tesis) => db.Tesis.Add(tesis);
}

