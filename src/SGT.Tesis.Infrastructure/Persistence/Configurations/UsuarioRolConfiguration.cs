using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="UsuarioRol"/> entity.
/// </summary>
public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("usuarios_roles");

        // Composite primary key matching DbContext definition
        builder.HasKey(ur => new { ur.UsuarioId, ur.RolId });

        builder.Property(ur => ur.Id)
            .HasColumnName("id");

        builder.Property(ur => ur.UsuarioId)
            .IsRequired()
            .HasColumnName("usuario_id");

        builder.Property(ur => ur.RolId)
            .IsRequired()
            .HasColumnName("rol_id");

        builder.Property(ur => ur.FechaAsignacion)
            .IsRequired()
            .HasColumnName("fecha_asignacion");

        builder.Property(ur => ur.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(ur => ur.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(ur => ur.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(ur => ur.Usuario)
            .WithMany(u => u.UsuarioRoles)
            .HasForeignKey(ur => ur.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ur => ur.Rol)
            .WithMany(r => r.UsuarioRoles)
            .HasForeignKey(ur => ur.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
