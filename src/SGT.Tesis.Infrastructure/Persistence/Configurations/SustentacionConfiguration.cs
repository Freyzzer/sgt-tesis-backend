using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Sustentacion"/> entity.
/// </summary>
public class SustentacionConfiguration : IEntityTypeConfiguration<Sustentacion>
{
    public void Configure(EntityTypeBuilder<Sustentacion> builder)
    {
        builder.ToTable("sustentaciones");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(s => s.FechaSustentacion)
            .IsRequired()
            .HasColumnName("fecha_sustentacion");

        builder.Property(s => s.HoraInicio)
            .IsRequired()
            .HasColumnName("hora_inicio");

        builder.Property(s => s.HoraFin)
            .HasColumnName("hora_fin");

        builder.Property(s => s.Lugar)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("lugar");

        builder.Property(s => s.Modalidad)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("modalidad");

        builder.Property(s => s.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado");

        builder.Property(s => s.CalificacionNumerica)
            .HasPrecision(4, 2)
            .HasColumnName("calificacion_numerica");

        builder.Property(s => s.Resultado)
            .HasMaxLength(50)
            .HasColumnName("resultado");

        builder.Property(s => s.ActaNumero)
            .HasMaxLength(50)
            .HasColumnName("acta_numero");

        builder.Property(s => s.Observaciones)
            .HasMaxLength(2000)
            .HasColumnName("observaciones");

        builder.Property(s => s.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(s => s.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(s => s.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Relationships
        builder.HasOne(s => s.ProyectoTesis)
            .WithMany()
            .HasForeignKey(s => s.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Jurados)
            .WithOne(j => j.Sustentacion)
            .HasForeignKey(j => j.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Resultados)
            .WithOne(r => r.Sustentacion)
            .HasForeignKey(r => r.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.SolicitudesDistincion)
            .WithOne(sd => sd.Sustentacion)
            .HasForeignKey(sd => sd.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
