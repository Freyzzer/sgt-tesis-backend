using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Docente"/> entity.
/// </summary>
public class DocenteConfiguration : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> builder)
    {
        builder.ToTable("docentes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.CodigoDocente)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("codigo_docente");

        builder.Property(d => d.Especialidad)
            .HasMaxLength(150)
            .HasColumnName("especialidad");

        builder.Property(d => d.TituloAcademico)
            .HasMaxLength(100)
            .HasColumnName("titulo_academico");

        builder.Property(d => d.UsuarioId)
            .IsRequired()
            .HasColumnName("usuario_id");

        builder.Property(d => d.LineaInvestigacionId)
            .HasColumnName("linea_investigacion_id");

        builder.Property(d => d.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(d => d.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(d => d.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(d => d.CodigoDocente)
            .IsUnique();

        builder.HasIndex(d => d.UsuarioId)
            .IsUnique();

        // Relationships
        builder.HasOne(d => d.Usuario)
            .WithOne(u => u.Docente)
            .HasForeignKey<Docente>(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.LineaInvestigacion)
            .WithMany(l => l.Docentes)
            .HasForeignKey(d => d.LineaInvestigacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
