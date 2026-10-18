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

    public Task<bool> ExisteActivaAsync(Guid studentId, CancellationToken cancellationToken) =>
        db.Tesis.AnyAsync(t => t.StudentId == studentId && t.Estado == EstadoTesis.EnCurso, cancellationToken);

    public void Agregar(TesisGrado tesis) => db.Tesis.Add(tesis);
}

/// <summary>
/// Sin requisitos documentales hasta que se defina el catálogo por etapa con el Product Owner
/// y exista la carga de documentos (US-07). Sustituir al implementar US-07.
/// </summary>
internal sealed class SinRequisitosDocumentales : IRequisitosDocumentales
{
    public Task<IReadOnlyList<string>> ObtenerFaltantesAsync(Guid etapaId, EtapaTipo tipo, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
