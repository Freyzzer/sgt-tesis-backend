using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Programa"/> entity.
/// </summary>
public class ProgramaConfiguration : IEntityTypeConfiguration<Programa>
{
    public void Configure(EntityTypeBuilder<Programa> builder)
    {
        builder.ToTable("programas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Codigo)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("codigo");

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("nombre");

        builder.Property(p => p.Descripcion)
            .HasMaxLength(500)
            .HasColumnName("descripcion");

        builder.Property(p => p.NivelFormacion)
            .HasMaxLength(50)
            .HasColumnName("nivel_formacion");

        builder.Property(p => p.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(p => p.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(p => p.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(p => p.Codigo)
            .IsUnique();

        // Relationships
        builder.HasMany(p => p.Cohortes)
            .WithOne(c => c.Programa)
            .HasForeignKey(c => c.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.LineasInvestigacion)
            .WithOne(l => l.Programa)
            .HasForeignKey(l => l.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Estudiantes)
            .WithOne(e => e.Programa)
            .HasForeignKey(e => e.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
