using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="ResultadoEvaluacion"/> entity.
/// </summary>
public class ResultadoEvaluacionConfiguration : IEntityTypeConfiguration<ResultadoEvaluacion>
{
    public void Configure(EntityTypeBuilder<ResultadoEvaluacion> builder)
    {
        builder.ToTable("resultados_evaluaciones");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(r => r.SustentacionId)
            .HasColumnName("sustentacion_id");

        builder.Property(r => r.EvaluadorId)
            .IsRequired()
            .HasColumnName("evaluador_id");

        builder.Property(r => r.Calificacion)
            .IsRequired()
            .HasPrecision(4, 2)
            .HasColumnName("calificacion");

        builder.Property(r => r.CriteriosJSON)
            .HasColumnName("criterios_json");

        builder.Property(r => r.Concepto)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("concepto");

        builder.Property(r => r.Observaciones)
            .HasMaxLength(2000)
            .HasColumnName("observaciones");

        builder.Property(r => r.FechaEvaluacion)
            .IsRequired()
            .HasColumnName("fecha_evaluacion");

        builder.Property(r => r.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(r => r.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(r => r.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Foreign Keys
        builder.HasOne(r => r.ProyectoTesis)
            .WithMany()
            .HasForeignKey(r => r.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Sustentacion)
            .WithMany(s => s.Resultados)
            .HasForeignKey(r => r.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Evaluador)
            .WithMany()
            .HasForeignKey(r => r.EvaluadorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
