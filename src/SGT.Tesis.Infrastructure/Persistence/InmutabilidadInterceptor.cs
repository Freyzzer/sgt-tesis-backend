using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;

namespace SGT.Tesis.Infrastructure.Persistence;

/// <summary>
/// Las decisiones son inmutables: cualquier intento de actualizarlas o borrarlas se rechaza antes de
/// llegar a la base de datos (que además restringe UPDATE/DELETE al usuario de la aplicación).
/// </summary>
public sealed class InmutabilidadInterceptor : SaveChangesInterceptor
{
    private static readonly Type[] Inmutables = [typeof(Decision)];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Verificar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Verificar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Verificar(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var infractora = context.ChangeTracker.Entries()
            .FirstOrDefault(e => e.State is EntityState.Modified or EntityState.Deleted && Inmutables.Contains(e.Entity.GetType()));

        if (infractora is not null)
        {
            throw new DomainException($"{infractora.Entity.GetType().Name} es inmutable: no se puede modificar ni eliminar.");
        }
    }
}
