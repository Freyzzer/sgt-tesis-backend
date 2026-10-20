using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Publicacion"/> entity.
/// </summary>
public class PublicacionConfiguration : IEntityTypeConfiguration<Publicacion>
{
    public void Configure(EntityTypeBuilder<Publicacion> builder)
    {
        builder.ToTable("publicaciones");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(p => p.Titulo)
            .IsRequired()
            .HasMaxLength(300)
            .HasColumnName("titulo");

        builder.Property(p => p.NombreRevistaOEvento)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("nombre_revista_o_evento");

        builder.Property(p => p.TipoPublicacion)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("tipo_publicacion");

        builder.Property(p => p.Indexacion)
            .HasMaxLength(100)
            .HasColumnName("indexacion");

        builder.Property(p => p.Doi)
            .HasMaxLength(100)
            .HasColumnName("doi");

        builder.Property(p => p.Url)
            .HasMaxLength(500)
            .HasColumnName("url");

        builder.Property(p => p.FechaPublicacion)
            .HasColumnName("fecha_publicacion");

        builder.Property(p => p.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado");

        builder.Property(p => p.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(p => p.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(p => p.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        builder.HasOne(p => p.ProyectoTesis)
            .WithMany()
            .HasForeignKey(p => p.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
