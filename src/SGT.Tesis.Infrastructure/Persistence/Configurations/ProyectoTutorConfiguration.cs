using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Projects;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="ProyectoTutor"/> entity.
/// </summary>
public class ProyectoTutorConfiguration : IEntityTypeConfiguration<ProyectoTutor>
{
    public void Configure(EntityTypeBuilder<ProyectoTutor> builder)
    {
        builder.ToTable("proyectos_tutores");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Id)
            .HasColumnName("id");

        builder.Property(pt => pt.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(pt => pt.DocenteId)
            .IsRequired()
            .HasColumnName("docente_id");

        builder.Property(pt => pt.RolTutor)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("rol_tutor");

        builder.Property(pt => pt.FechaAsignacion)
            .IsRequired()
            .HasColumnName("fecha_asignacion");

        builder.Property(pt => pt.FechaRetiro)
            .HasColumnName("fecha_retiro");

        builder.Property(pt => pt.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(pt => pt.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(pt => pt.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(pt => new { pt.ProyectoTesisId, pt.DocenteId })
            .IsUnique();

        // Relationships
        builder.HasOne(pt => pt.ProyectoTesis)
            .WithMany(p => p.Tutores)
            .HasForeignKey(pt => pt.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.Docente)
            .WithMany()
            .HasForeignKey(pt => pt.DocenteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
