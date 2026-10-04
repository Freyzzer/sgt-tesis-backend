using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Security;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Rol"/> entity.
/// </summary>
public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.Nombre)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("nombre");

        builder.Property(r => r.Descripcion)
            .HasMaxLength(250)
            .HasColumnName("descripcion");

        builder.Property(r => r.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(r => r.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(r => r.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(r => r.Nombre)
            .IsUnique();

        // Relationships
        builder.HasMany(r => r.UsuarioRoles)
            .WithOne(ur => ur.Rol)
            .HasForeignKey(ur => ur.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
