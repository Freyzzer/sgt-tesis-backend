using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombres).IsRequired().HasMaxLength(Usuario.NombresMaxLength);
        builder.Property(u => u.Apellidos).IsRequired().HasMaxLength(Usuario.ApellidosMaxLength);
        builder.Property(u => u.Correo).IsRequired().HasMaxLength(Usuario.CorreoMaxLength);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(Usuario.PasswordHashMaxLength);
        builder.Property(u => u.Rol).IsRequired().HasConversion<int>();
        builder.Property(u => u.Activo).IsRequired();

        builder.HasIndex(u => u.Correo).IsUnique();
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(RefreshToken.HashMaxLength);
        builder.Property(t => t.ExpiraEn).IsRequired();

        builder.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamiliaId);
        builder.HasIndex(t => t.UsuarioId);
    }
}
