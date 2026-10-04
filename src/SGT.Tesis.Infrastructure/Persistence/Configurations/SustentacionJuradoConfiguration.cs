using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="SustentacionJurado"/> entity.
/// </summary>
public class SustentacionJuradoConfiguration : IEntityTypeConfiguration<SustentacionJurado>
{
    public void Configure(EntityTypeBuilder<SustentacionJurado> builder)
    {
        builder.ToTable("sustentaciones_jurados");

        builder.HasKey(sj => sj.Id);

        builder.Property(sj => sj.Id)
            .HasColumnName("id");

        builder.Property(sj => sj.SustentacionId)
            .IsRequired()
            .HasColumnName("sustentacion_id");

        builder.Property(sj => sj.DocenteId)
            .IsRequired()
            .HasColumnName("docente_id");

        builder.Property(sj => sj.RolJurado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("rol_jurado");

        builder.Property(sj => sj.Asistio)
            .IsRequired()
            .HasColumnName("asistio");

        builder.Property(sj => sj.Confirmado)
            .IsRequired()
            .HasColumnName("confirmado");

        builder.Property(sj => sj.FechaAsignacion)
            .IsRequired()
            .HasColumnName("fecha_asignacion");

        builder.Property(sj => sj.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(sj => sj.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(sj => sj.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(sj => new { sj.SustentacionId, sj.DocenteId })
            .IsUnique();

        // Relationships
        builder.HasOne(sj => sj.Sustentacion)
            .WithMany(s => s.Jurados)
            .HasForeignKey(sj => sj.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sj => sj.Docente)
            .WithMany()
            .HasForeignKey(sj => sj.DocenteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
