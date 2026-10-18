using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TesisGrado> Tesis => Set<TesisGrado>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entity in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            var builder = modelBuilder.Entity(entity.ClrType);
            builder.Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
            builder.Property(nameof(BaseEntity.CreatedAt)).IsRequired();
        }
    }
}

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            // Carrera entre dos solicitudes: el índice único es la última defensa.
            throw new ConflictoException("Ya existe un registro con esos datos.");
        }
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException ex) => ex.InnerException switch
    {
        SqlException sql => sql.Number is 2601 or 2627,
        { } inner => inner.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };
}
