using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Estudiante"/> entity.
/// </summary>
public class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        builder.ToTable("estudiantes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.CodigoEstudiantil)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("codigo_estudiantil");

        builder.Property(e => e.EstadoAcademico)
            .HasMaxLength(50)
            .HasColumnName("estado_academico");

        builder.Property(e => e.UsuarioId)
            .IsRequired()
            .HasColumnName("usuario_id");

        builder.Property(e => e.ProgramaId)
            .IsRequired()
            .HasColumnName("programa_id");

        builder.Property(e => e.CohorteId)
            .HasColumnName("cohorte_id");

        builder.Property(e => e.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(e => e.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(e => e.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(e => e.CodigoEstudiantil)
            .IsUnique();

        builder.HasIndex(e => e.UsuarioId)
            .IsUnique();

        // Relationships
        builder.HasOne(e => e.Usuario)
            .WithOne(u => u.Estudiante)
            .HasForeignKey<Estudiante>(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Programa)
            .WithMany(p => p.Estudiantes)
            .HasForeignKey(e => e.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Cohorte)
            .WithMany(c => c.Estudiantes)
            .HasForeignKey(e => e.CohorteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
