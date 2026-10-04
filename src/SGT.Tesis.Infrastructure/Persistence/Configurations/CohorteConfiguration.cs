using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Cohorte"/> entity.
/// </summary>
public class CohorteConfiguration : IEntityTypeConfiguration<Cohorte>
{
    public void Configure(EntityTypeBuilder<Cohorte> builder)
    {
        builder.ToTable("cohortes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("nombre");

        builder.Property(c => c.Periodo)
            .IsRequired()
            .HasColumnName("periodo");

        builder.Property(c => c.FechaInicio)
            .IsRequired()
            .HasColumnName("fecha_inicio");

        builder.Property(c => c.FechaFin)
            .HasColumnName("fecha_fin");

        builder.Property(c => c.ProgramaId)
            .IsRequired()
            .HasColumnName("programa_id");

        builder.Property(c => c.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(c => c.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(c => c.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(c => c.Programa)
            .WithMany(p => p.Cohortes)
            .HasForeignKey(c => c.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Estudiantes)
            .WithOne(e => e.Cohorte)
            .HasForeignKey(e => e.CohorteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
