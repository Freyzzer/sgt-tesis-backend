using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class AsignacionEvaluadorConfiguration : IEntityTypeConfiguration<AsignacionEvaluador>
{
    public void Configure(EntityTypeBuilder<AsignacionEvaluador> builder)
    {
        builder.ToTable("AsignacionesEvaluador");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Activa).IsRequired();
        builder.Property(a => a.FechaAsignacion).IsRequired();

        builder.HasOne<Etapa>().WithMany().HasForeignKey(a => a.EtapaId).OnDelete(DeleteBehavior.Restrict);

        // Un evaluador no puede tener dos asignaciones activas en la misma etapa.
        builder.HasIndex(a => new { a.EtapaId, a.EvaluadorId })
            .IsUnique()
            .HasFilter("[Activa] = 1")
            .HasDatabaseName("UX_Asignaciones_Etapa_Evaluador_Activa");
        builder.HasIndex(a => a.EvaluadorId);
    }
}

internal sealed class VeredictoConfiguration : IEntityTypeConfiguration<Veredicto>
{
    public void Configure(EntityTypeBuilder<Veredicto> builder)
    {
        builder.ToTable("Veredictos");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Concepto).IsRequired().HasConversion<int>();
        builder.Property(v => v.Comentarios).IsRequired().HasMaxLength(Veredicto.ComentariosMaxLength);
        builder.Property(v => v.Version).IsRequired();

        builder.HasOne<Etapa>().WithMany().HasForeignKey(v => v.EtapaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.EtapaId, v.EvaluadorId, v.Version }).IsUnique();
    }
}

internal sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("Decisiones");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Resultado).IsRequired().HasConversion<int>();
        builder.Property(d => d.Justificacion).HasMaxLength(Decision.JustificacionMaxLength);
        builder.Property(d => d.FechaUtc).IsRequired();

        builder.HasOne<Etapa>().WithMany().HasForeignKey(d => d.EtapaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.EtapaId);
    }
}
