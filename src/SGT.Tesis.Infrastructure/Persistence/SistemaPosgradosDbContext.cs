using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Audit;
using SGT.Tesis.Domain.Entities.Documents;
using SGT.Tesis.Domain.Entities.Evaluation;
using SGT.Tesis.Domain.Entities.Projects;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence;

public class SistemaPosgradosDbContext : DbContext
{
    public SistemaPosgradosDbContext(DbContextOptions<SistemaPosgradosDbContext> options)
        : base(options)
    {
    }

    // Entidades Académicas
    public DbSet<Cohorte> Cohortes => Set<Cohorte>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<LineaInvestigacion> LineasInvestigacion => Set<LineaInvestigacion>();
    public DbSet<Programa> Programas => Set<Programa>();

    // Entidades de Auditoría
    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();

    // Entidades de Documentos
    public DbSet<ConsentimientoUsuario> ConsentimientosUsuario => Set<ConsentimientoUsuario>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoLegal> DocumentosLegales => Set<DocumentoLegal>();
    public DbSet<Revision> Revisiones => Set<Revision>();
    public DbSet<VersionDocumento> VersionesDocumento => Set<VersionDocumento>();

    // Entidades de Evaluación
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();
    public DbSet<ResultadoEvaluacion> ResultadosEvaluaciones => Set<ResultadoEvaluacion>();
    public DbSet<SolicitudDistincion> SolicitudesDistincion => Set<SolicitudDistincion>();
    public DbSet<Sustentacion> Sustentaciones => Set<Sustentacion>();
    public DbSet<SustentacionJurado> SustentacionesJurados => Set<SustentacionJurado>();

    // Entidades de Proyectos
    public DbSet<ProyectoTesis> ProyectosTesis => Set<ProyectoTesis>();
    public DbSet<ProyectoTutor> ProyectosTutores => Set<ProyectoTutor>();

    // Entidades de Seguridad
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Claves compuestas
        // ProyectoTutor: (ProyectoID / ProyectoTesisId, DocenteID, FechaAsignacion)
        modelBuilder.Entity<ProyectoTutor>(entity =>
        {
            entity.HasKey(pt => new { pt.ProyectoTesisId, pt.DocenteId, pt.FechaAsignacion });
        });

        // SustentacionJurado: (SustentacionID, DocenteID / UsuarioID)
        modelBuilder.Entity<SustentacionJurado>(entity =>
        {
            entity.HasKey(sj => new { sj.SustentacionId, sj.DocenteId });
        });

        // UsuarioRol: (UsuarioId, RolId)
        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.HasKey(ur => new { ur.UsuarioId, ur.RolId });
        });

        // 2. Relaciones 1:1 (PK = FK)
        // Estudiante (EstudianteID / UsuarioId -> Usuario.UsuarioID)
        modelBuilder.Entity<Estudiante>(entity =>
        {
            entity.HasOne(e => e.Usuario)
                  .WithOne(u => u.Estudiante)
                  .HasForeignKey<Estudiante>(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Docente (DocenteID / UsuarioId -> Usuario.UsuarioID)
        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasOne(d => d.Usuario)
                  .WithOne(u => u.Docente)
                  .HasForeignKey<Docente>(d => d.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ResultadoEvaluacion (SustentacionID -> Sustentacion.SustentacionID)
        modelBuilder.Entity<ResultadoEvaluacion>(entity =>
        {
            entity.HasOne(r => r.Sustentacion)
                  .WithOne()
                  .HasForeignKey<ResultadoEvaluacion>(r => r.SustentacionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Aplicar configuraciones de Fluent API en el ensamblado si existen
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SistemaPosgradosDbContext).Assembly);

        // 4. Configurar DeleteBehavior.Restrict de manera global en todas las llaves foráneas no-propietarias para evitar cascadas destructivas
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (!foreignKey.IsOwnership && foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
