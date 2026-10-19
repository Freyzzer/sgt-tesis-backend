using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Auditoria;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Domain.Notificaciones;

namespace SGT.Tesis.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TesisGrado> Tesis => Set<TesisGrado>();

    public DbSet<Etapa> Etapas => Set<Etapa>();

    public DbSet<Documento> Documentos => Set<Documento>();

    public DbSet<AsignacionEvaluador> Asignaciones => Set<AsignacionEvaluador>();

    public DbSet<Veredicto> Veredictos => Set<Veredicto>();

    public DbSet<Decision> Decisiones => Set<Decision>();

    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public DbSet<NotificacionPendiente> Notificaciones => Set<NotificacionPendiente>();

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
        catch (DbUpdateConcurrencyException)
        {
            // Otra solicitud cambió el mismo registro (por ejemplo, dos decisiones simultáneas sobre una etapa).
            throw new ConflictoException("El recurso fue modificado por otra solicitud. Vuelva a consultarlo e intente de nuevo.");
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
