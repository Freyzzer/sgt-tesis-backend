using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Documents;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="ConsentimientoUsuario"/> entity.
/// </summary>
public class ConsentimientoUsuarioConfiguration : IEntityTypeConfiguration<ConsentimientoUsuario>
{
    public void Configure(EntityTypeBuilder<ConsentimientoUsuario> builder)
    {
        builder.ToTable("consentimientos_usuarios");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(c => c.UsuarioId)
            .IsRequired()
            .HasColumnName("usuario_id");

        builder.Property(c => c.DocumentoLegalId)
            .IsRequired()
            .HasColumnName("documento_legal_id");

        builder.Property(c => c.Aceptado)
            .IsRequired()
            .HasColumnName("aceptado");

        builder.Property(c => c.FechaAceptacion)
            .IsRequired()
            .HasColumnName("fecha_aceptacion");

        builder.Property(c => c.DireccionIp)
            .HasMaxLength(45)
            .HasColumnName("direccion_ip");

        builder.Property(c => c.UserAgent)
            .HasMaxLength(500)
            .HasColumnName("user_agent");

        builder.Property(c => c.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(c => c.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(c => c.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(c => new { c.UsuarioId, c.DocumentoLegalId })
            .IsUnique();

        // Relationships
        builder.HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DocumentoLegal)
            .WithMany()
            .HasForeignKey(c => c.DocumentoLegalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
