using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Documents;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="DocumentoLegal"/> entity.
/// </summary>
public class DocumentoLegalConfiguration : IEntityTypeConfiguration<DocumentoLegal>
{
    public void Configure(EntityTypeBuilder<DocumentoLegal> builder)
    {
        builder.ToTable("documentos_legales");

        builder.HasKey(dl => dl.Id);

        builder.Property(dl => dl.Id)
            .HasColumnName("id");

        builder.Property(dl => dl.Titulo)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("titulo");

        builder.Property(dl => dl.Tipo)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("tipo");

        builder.Property(dl => dl.Contenido)
            .IsRequired()
            .HasColumnName("contenido");

        builder.Property(dl => dl.Version)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("version");

        builder.Property(dl => dl.FechaPublicacion)
            .IsRequired()
            .HasColumnName("fecha_publicacion");

        builder.Property(dl => dl.EsObligatorio)
            .IsRequired()
            .HasColumnName("es_obligatorio");

        builder.Property(dl => dl.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(dl => dl.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(dl => dl.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(dl => new { dl.Tipo, dl.Version })
            .IsUnique();

        // Relationships
        builder.HasMany(dl => dl.Consentimientos)
            .WithOne(c => c.DocumentoLegal)
            .HasForeignKey(c => c.DocumentoLegalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
