using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="ProyectoTesis"/> entity.
/// </summary>
public class ProyectoTesisConfiguration : IEntityTypeConfiguration<ProyectoTesis>
{
    public void Configure(EntityTypeBuilder<ProyectoTesis> builder)
    {
        builder.ToTable("proyectos_tesis");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Id)
            .HasColumnName("id");

        builder.Property(pt => pt.Titulo)
            .IsRequired()
            .HasMaxLength(300)
            .HasColumnName("titulo");

        builder.Property(pt => pt.Resumen)
            .HasMaxLength(2000)
            .HasColumnName("resumen");

        builder.Property(pt => pt.Modalidad)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("modalidad");

        builder.Property(pt => pt.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado");

        builder.Property(pt => pt.FechaFormulacion)
            .IsRequired()
            .HasColumnName("fecha_formulacion");

        builder.Property(pt => pt.FechaInicio)
            .HasColumnName("fecha_inicio");

        builder.Property(pt => pt.FechaFin)
            .HasColumnName("fecha_fin");

        builder.Property(pt => pt.FechaAprobacion)
            .HasColumnName("fecha_aprobacion");

        builder.Property(pt => pt.EstudianteId)
            .IsRequired()
            .HasColumnName("estudiante_id");

        builder.Property(pt => pt.LineaInvestigacionId)
            .IsRequired()
            .HasColumnName("linea_investigacion_id");

        builder.Property(pt => pt.DirectorId)
            .HasColumnName("director_id");

        builder.Property(pt => pt.CodirectorId)
            .HasColumnName("codirector_id");

        builder.Property(pt => pt.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(pt => pt.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(pt => pt.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(pt => pt.Estudiante)
            .WithMany()
            .HasForeignKey(pt => pt.EstudianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.LineaInvestigacion)
            .WithMany()
            .HasForeignKey(pt => pt.LineaInvestigacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.Director)
            .WithMany()
            .HasForeignKey(pt => pt.DirectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.Codirector)
            .WithMany()
            .HasForeignKey(pt => pt.CodirectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.Tutores)
            .WithOne(t => t.ProyectoTesis)
            .HasForeignKey(t => t.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.Documentos)
            .WithOne(d => d.ProyectoTesis)
            .HasForeignKey(d => d.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.Sustentaciones)
            .WithOne(s => s.ProyectoTesis)
            .HasForeignKey(s => s.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.ResultadosEvaluacion)
            .WithOne(re => re.ProyectoTesis)
            .HasForeignKey(re => re.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.SolicitudesDistincion)
            .WithOne(sd => sd.ProyectoTesis)
            .HasForeignKey(sd => sd.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pt => pt.Publicaciones)
            .WithOne(p => p.ProyectoTesis)
            .HasForeignKey(p => p.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
