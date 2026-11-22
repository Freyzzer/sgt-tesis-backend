using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Infrastructure.Persistence;

/// <summary>Completa CreatedAt/By y UpdatedAt/By de toda <see cref="BaseEntity"/> al guardar.</summary>
public sealed class AuditoriaInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Aplicar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Aplicar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Aplicar(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var ahora = clock.UtcNow;
        var usuario = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(ahora, usuario);
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkUpdated(ahora, usuario);
                    break;
            }
        }
    }
}
