using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("Documentos");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Categoria).IsRequired().HasMaxLength(Documento.CategoriaMaxLength);
        builder.Property(d => d.CategoriaClave).IsRequired().HasMaxLength(Documento.CategoriaMaxLength);
        builder.Property(d => d.NombreOriginal).IsRequired().HasMaxLength(Documento.NombreMaxLength);
        builder.Property(d => d.NombreAlmacenado).IsRequired().HasMaxLength(Documento.NombreAlmacenadoMaxLength);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(Documento.ContentTypeMaxLength);
        builder.Property(d => d.HashSha256).IsRequired().HasMaxLength(Documento.HashLength).IsFixedLength();
        builder.Property(d => d.TamanoBytes).IsRequired();
        builder.Property(d => d.Version).IsRequired();

        builder.HasOne<Etapa>().WithMany().HasForeignKey(d => d.EtapaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.EtapaId, d.CategoriaClave, d.Version }).IsUnique();
        builder.HasIndex(d => d.NombreAlmacenado).IsUnique();
    }
}
