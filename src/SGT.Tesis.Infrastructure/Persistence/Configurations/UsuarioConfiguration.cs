using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Academic;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Usuario"/> entity.
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Nombres)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("nombres");

        builder.Property(u => u.Apellidos)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("apellidos");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("email");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("password_hash");

        builder.Property(u => u.DocumentoIdentidad)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("documento_identidad");

        builder.Property(u => u.Telefono)
            .HasMaxLength(20)
            .HasColumnName("telefono");

        builder.Property(u => u.UltimoAcceso)
            .HasColumnName("ultimo_acceso");

        builder.Property(u => u.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(u => u.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(u => u.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.DocumentoIdentidad)
            .IsUnique();

        // Relationships
        builder.HasMany(u => u.UsuarioRoles)
            .WithOne(ur => ur.Usuario)
            .HasForeignKey(ur => ur.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Sesiones)
            .WithOne(s => s.Usuario)
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Estudiante)
            .WithOne(e => e.Usuario)
            .HasForeignKey<Estudiante>(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Docente)
            .WithOne(d => d.Usuario)
            .HasForeignKey<Docente>(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
