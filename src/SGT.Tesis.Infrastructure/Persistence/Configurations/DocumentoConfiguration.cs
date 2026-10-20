using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Documents;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Documento"/> entity.
/// </summary>
public class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documentos");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(d => d.TipoDocumento)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("tipo_documento");

        builder.Property(d => d.Titulo)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("titulo");

        builder.Property(d => d.Descripcion)
            .HasMaxLength(1000)
            .HasColumnName("descripcion");

        builder.Property(d => d.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado");

        builder.Property(d => d.Etapa)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("etapa");

        builder.Property(d => d.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(d => d.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(d => d.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(d => d.ProyectoTesis)
            .WithMany(p => p.Documentos)
            .HasForeignKey(d => d.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Versiones)
            .WithOne(v => v.Documento)
            .HasForeignKey(v => v.DocumentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
