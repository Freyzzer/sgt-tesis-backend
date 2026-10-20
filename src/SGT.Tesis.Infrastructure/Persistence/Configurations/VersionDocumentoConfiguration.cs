using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Documents;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="VersionDocumento"/> entity.
/// </summary>
public class VersionDocumentoConfiguration : IEntityTypeConfiguration<VersionDocumento>
{
    public void Configure(EntityTypeBuilder<VersionDocumento> builder)
    {
        builder.ToTable("versiones_documentos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(v => v.DocumentoId)
            .IsRequired()
            .HasColumnName("documento_id");

        builder.Property(v => v.NumeroVersion)
            .IsRequired()
            .HasColumnName("numero_version");

        builder.Property(v => v.NombreArchivo)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("nombre_archivo");

        builder.Property(v => v.BucketMinio)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("bucket_minio");

        builder.Property(v => v.ObjectKeyMinio)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("object_key_minio");

        builder.Property(v => v.RutaMinio)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("ruta_minio");

        builder.Property(v => v.Extension)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("extension");

        builder.Property(v => v.TamanoBytes)
            .IsRequired()
            .HasColumnName("tamano_bytes");

        builder.Property(v => v.HashArchivo)
            .IsRequired()
            .HasMaxLength(128)
            .HasColumnName("hash_archivo");

        builder.Property(v => v.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("content_type");

        builder.Property(v => v.SubidoPorId)
            .IsRequired()
            .HasColumnName("subido_por_id");

        builder.Property(v => v.FechaSubida)
            .IsRequired()
            .HasColumnName("fecha_subida");

        builder.Property(v => v.Comentarios)
            .HasMaxLength(1000)
            .HasColumnName("comentarios");

        builder.Property(v => v.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(v => v.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(v => v.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(v => new { v.DocumentoId, v.NumeroVersion })
            .IsUnique();

        // Relationships
        builder.HasOne(v => v.Documento)
            .WithMany(d => d.Versiones)
            .HasForeignKey(v => v.DocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.SubidoPor)
            .WithMany()
            .HasForeignKey(v => v.SubidoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Revisiones)
            .WithOne(r => r.VersionDocumento)
            .HasForeignKey(r => r.VersionDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
