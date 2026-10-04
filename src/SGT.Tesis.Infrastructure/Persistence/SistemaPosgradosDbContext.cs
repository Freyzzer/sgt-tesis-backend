using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Audit;
using SGT.Tesis.Domain.Entities.Documents;
using SGT.Tesis.Domain.Entities.Evaluation;
using SGT.Tesis.Domain.Entities.Projects;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence;

/// <summary>
/// Database context for the Postgraduate System, managing database entities, configurations, and relationships.
/// </summary>
public class SistemaPosgradosDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SistemaPosgradosDbContext"/> class using the specified options.
    /// </summary>
    /// <param name="options">The options to be used by this context.</param>
    public SistemaPosgradosDbContext(DbContextOptions<SistemaPosgradosDbContext> options)
        : base(options)
    {
    }

    // Academic Entities
    /// <summary>
    /// Gets or sets the cohort entities.
    /// </summary>
    public DbSet<Cohorte> Cohortes => Set<Cohorte>();

    /// <summary>
    /// Gets or sets the professor entities.
    /// </summary>
    public DbSet<Docente> Docentes => Set<Docente>();

    /// <summary>
    /// Gets or sets the student entities.
    /// </summary>
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();

    /// <summary>
    /// Gets or sets the research line entities.
    /// </summary>
    public DbSet<LineaInvestigacion> LineasInvestigacion => Set<LineaInvestigacion>();

    /// <summary>
    /// Gets or sets the academic program entities.
    /// </summary>
    public DbSet<Programa> Programas => Set<Programa>();

    // Audit Entities
    /// <summary>
    /// Gets or sets the audit event entities.
    /// </summary>
    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();

    // Document Entities
    /// <summary>
    /// Gets or sets the user consent entities.
    /// </summary>
    public DbSet<ConsentimientoUsuario> ConsentimientosUsuario => Set<ConsentimientoUsuario>();

    /// <summary>
    /// Gets or sets the document entities.
    /// </summary>
    public DbSet<Documento> Documentos => Set<Documento>();

    /// <summary>
    /// Gets or sets the legal document entities.
    /// </summary>
    public DbSet<DocumentoLegal> DocumentosLegales => Set<DocumentoLegal>();

    /// <summary>
    /// Gets or sets the review entities.
    /// </summary>
    public DbSet<Revision> Revisiones => Set<Revision>();

    /// <summary>
    /// Gets or sets the document version entities.
    /// </summary>
    public DbSet<VersionDocumento> VersionesDocumento => Set<VersionDocumento>();

    // Evaluation Entities
    /// <summary>
    /// Gets or sets the publication entities.
    /// </summary>
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();

    /// <summary>
    /// Gets or sets the evaluation result entities.
    /// </summary>
    public DbSet<ResultadoEvaluacion> ResultadosEvaluaciones => Set<ResultadoEvaluacion>();

    /// <summary>
    /// Gets or sets the distinction request entities.
    /// </summary>
    public DbSet<SolicitudDistincion> SolicitudesDistincion => Set<SolicitudDistincion>();

    /// <summary>
    /// Gets or sets the thesis defense entities.
    /// </summary>
    public DbSet<Sustentacion> Sustentaciones => Set<Sustentacion>();

    /// <summary>
    /// Gets or sets the defense jury assignment entities.
    /// </summary>
    public DbSet<SustentacionJurado> SustentacionesJurados => Set<SustentacionJurado>();

    // Project Entities
    /// <summary>
    /// Gets or sets the thesis project entities.
    /// </summary>
    public DbSet<ProyectoTesis> ProyectosTesis => Set<ProyectoTesis>();

    /// <summary>
    /// Gets or sets the project tutor assignment entities.
    /// </summary>
    public DbSet<ProyectoTutor> ProyectosTutores => Set<ProyectoTutor>();

    // Security Entities
    /// <summary>
    /// Gets or sets the role entities.
    /// </summary>
    public DbSet<Rol> Roles => Set<Rol>();

    /// <summary>
    /// Gets or sets the user session entities.
    /// </summary>
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();

    /// <summary>
    /// Gets or sets the user entities.
    /// </summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>
    /// Gets or sets the user-role mapping entities.
    /// </summary>
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Composite keys
        // ProyectoTutor: (ProyectoTesisId, DocenteId, FechaAsignacion)
        modelBuilder.Entity<ProyectoTutor>(entity =>
        {
            entity.HasKey(pt => new { pt.ProyectoTesisId, pt.DocenteId, pt.FechaAsignacion });
        });

        // SustentacionJurado: (SustentacionId, DocenteId)
        modelBuilder.Entity<SustentacionJurado>(entity =>
        {
            entity.HasKey(sj => new { sj.SustentacionId, sj.DocenteId });
        });

        // UsuarioRol: (UsuarioId, RolId)
        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.HasKey(ur => new { ur.UsuarioId, ur.RolId });
        });


        // 2. 1:1 Relationships (PK = FK)
        // Estudiante (UsuarioId -> Usuario.UsuarioId)
        modelBuilder.Entity<Estudiante>(entity =>
        {
            entity.HasOne(e => e.Usuario)
                  .WithOne(u => u.Estudiante)
                  .HasForeignKey<Estudiante>(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Docente (UsuarioId -> Usuario.UsuarioId)
        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasOne(d => d.Usuario)
                  .WithOne(u => u.Docente)
                  .HasForeignKey<Docente>(d => d.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ResultadoEvaluacion (SustentacionId -> Sustentacion.SustentacionId)
        modelBuilder.Entity<ResultadoEvaluacion>(entity =>
        {
            entity.HasOne(r => r.Sustentacion)
                  .WithOne()
                  .HasForeignKey<ResultadoEvaluacion>(r => r.SustentacionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. Apply Fluent API configurations from assembly if any exist
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SistemaPosgradosDbContext).Assembly);

        // 4. Globally configure DeleteBehavior.Restrict on all non-ownership foreign keys to prevent destructive cascades
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (!foreignKey.IsOwnership && foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
