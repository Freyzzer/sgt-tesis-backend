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
        builder.Property(t => t.Estado).IsRequired().HasConversion<int>();

        builder.HasMany(t => t.Etapas)
            .WithOne()
            .HasForeignKey(e => e.TesisId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.StudentId);

        // Un estudiante solo puede tener una tesis en curso (EstadoTesis.EnCurso = 1).
        builder.HasIndex(t => t.StudentId)
            .IsUnique()
            .HasFilter("[Estado] = 1")
            .HasDatabaseName("UX_Tesis_StudentId_EnCurso");
    }
}

internal sealed class EtapaConfiguration : IEntityTypeConfiguration<Etapa>
{
    public void Configure(EntityTypeBuilder<Etapa> builder)
    {
        builder.ToTable("Etapas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Tipo).IsRequired().HasConversion<int>();
        builder.Property(e => e.Estado).IsRequired().HasConversion<int>();
        builder.Property(e => e.FechaApertura).IsRequired();

        builder.Property(e => e.Ronda).IsRequired();
        builder.Property(e => e.Revision).IsRequired().IsConcurrencyToken();

        builder.Ignore(e => e.EstaAbierta);
        builder.Ignore(e => e.AdmiteDocumentos);

        builder.HasIndex(e => new { e.TesisId, e.Tipo }).IsUnique();
    }
}
