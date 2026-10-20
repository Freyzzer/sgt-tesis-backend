using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Evaluation;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="SolicitudDistincion"/> entity.
/// </summary>
public class SolicitudDistincionConfiguration : IEntityTypeConfiguration<SolicitudDistincion>
{
    public void Configure(EntityTypeBuilder<SolicitudDistincion> builder)
    {
        builder.ToTable("solicitudes_distinciones");

        builder.HasKey(sd => sd.Id);

        builder.Property(sd => sd.Id)
            .HasColumnName("id");

        builder.Property(sd => sd.ProyectoTesisId)
            .IsRequired()
            .HasColumnName("proyecto_tesis_id");

        builder.Property(sd => sd.SustentacionId)
            .HasColumnName("sustentacion_id");

        builder.Property(sd => sd.TipoDistincion)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("tipo_distincion");

        builder.Property(sd => sd.Justificacion)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnName("justificacion");

        builder.Property(sd => sd.SolicitadoPorId)
            .IsRequired()
            .HasColumnName("solicitado_por_id");

        builder.Property(sd => sd.Estado)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("estado");

        builder.Property(sd => sd.AprobadoPorConsejo)
            .IsRequired()
            .HasColumnName("aprobado_por_consejo");

        builder.Property(sd => sd.FechaSolicitud)
            .IsRequired()
            .HasColumnName("fecha_solicitud");

        builder.Property(sd => sd.FechaResolucion)
            .HasColumnName("fecha_resolucion");

        builder.Property(sd => sd.NumeroResolucion)
            .HasMaxLength(50)
            .HasColumnName("numero_resolucion");

        builder.Property(sd => sd.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(sd => sd.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(sd => sd.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Foreign Keys
        builder.HasOne(sd => sd.ProyectoTesis)
            .WithMany()
            .HasForeignKey(sd => sd.ProyectoTesisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sd => sd.Sustentacion)
            .WithMany(s => s.SolicitudesDistincion)
            .HasForeignKey(sd => sd.SustentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sd => sd.SolicitadoPor)
            .WithMany()
            .HasForeignKey(sd => sd.SolicitadoPorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
