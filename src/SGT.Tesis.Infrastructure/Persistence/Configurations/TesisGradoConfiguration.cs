using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class TesisGradoConfiguration : IEntityTypeConfiguration<TesisGrado>
{
    public void Configure(EntityTypeBuilder<TesisGrado> builder)
    {
        builder.ToTable("Tesis");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.StudentId).IsRequired();
        builder.Property(t => t.Titulo).IsRequired().HasMaxLength(TesisGrado.TituloMaxLength);
        builder.Property(t => t.Resumen).IsRequired().HasMaxLength(TesisGrado.ResumenMaxLength);

        builder.HasIndex(t => t.StudentId);
    }
}
