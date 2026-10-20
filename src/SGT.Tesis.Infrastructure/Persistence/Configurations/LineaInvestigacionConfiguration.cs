using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="LineaInvestigacion"/> entity.
/// </summary>
public class LineaInvestigacionConfiguration : IEntityTypeConfiguration<LineaInvestigacion>
{
    public void Configure(EntityTypeBuilder<LineaInvestigacion> builder)
    {
        builder.ToTable("lineas_investigacion");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Id)
            .HasColumnName("id");

        builder.Property(li => li.Codigo)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("codigo");

        builder.Property(li => li.Nombre)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("nombre");

        builder.Property(li => li.Descripcion)
            .HasMaxLength(500)
            .HasColumnName("descripcion");

        builder.Property(li => li.ProgramaId)
            .IsRequired()
            .HasColumnName("programa_id");

        builder.Property(li => li.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(li => li.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(li => li.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(li => li.Codigo)
            .IsUnique();

        // Relationships
        builder.HasOne(li => li.Programa)
            .WithMany(p => p.LineasInvestigacion)
            .HasForeignKey(li => li.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(li => li.Docentes)
            .WithOne(d => d.LineaInvestigacion)
            .HasForeignKey(d => d.LineaInvestigacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
