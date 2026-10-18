using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TesisGrado> Tesis => Set<TesisGrado>();

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
