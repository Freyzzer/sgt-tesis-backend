using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Documents;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Revision"/> entity.
/// </summary>
public class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.ToTable("revisiones");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.VersionDocumentoId)
            .IsRequired()
            .HasColumnName("version_documento_id");

        builder.Property(r => r.RevisorId)
            .IsRequired()
            .HasColumnName("revisor_id");

        builder.Property(r => r.EstadoRevision)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado_revision");

        builder.Property(r => r.Observaciones)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnName("observaciones");

        builder.Property(r => r.RutaArchivoCorrecciones)
            .HasMaxLength(500)
            .HasColumnName("ruta_archivo_correcciones");

        builder.Property(r => r.FechaRevision)
            .HasColumnName("fecha_revision");

        builder.Property(r => r.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(r => r.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(r => r.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(r => r.VersionDocumento)
            .WithMany(v => v.Revisiones)
            .HasForeignKey(r => r.VersionDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Revisor)
            .WithMany()
            .HasForeignKey(r => r.RevisorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
