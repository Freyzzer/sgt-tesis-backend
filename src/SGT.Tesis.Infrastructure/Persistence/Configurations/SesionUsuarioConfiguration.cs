using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="SesionUsuario"/> entity.
/// </summary>
public class SesionUsuarioConfiguration : IEntityTypeConfiguration<SesionUsuario>
{
    public void Configure(EntityTypeBuilder<SesionUsuario> builder)
    {
        builder.ToTable("sesiones_usuario");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.UsuarioId)
            .IsRequired()
            .HasColumnName("usuario_id");

        builder.Property(s => s.RefreshToken)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("refresh_token");

        builder.Property(s => s.FechaExpiracion)
            .IsRequired()
            .HasColumnName("fecha_expiracion");

        builder.Property(s => s.EsRevocado)
            .IsRequired()
            .HasColumnName("es_revocado");

        builder.Property(s => s.DireccionIp)
            .HasMaxLength(45)
            .HasColumnName("direccion_ip");

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500)
            .HasColumnName("user_agent");

        builder.Property(s => s.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(s => s.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(s => s.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(s => s.RefreshToken)
            .IsUnique();

        builder.HasIndex(s => s.UsuarioId);

        // Relationships
        builder.HasOne(s => s.Usuario)
            .WithMany(u => u.Sesiones)
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
