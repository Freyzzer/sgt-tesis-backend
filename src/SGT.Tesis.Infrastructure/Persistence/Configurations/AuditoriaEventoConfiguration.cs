using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Entities.Audit;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="AuditoriaEvento"/> entity.
/// </summary>
public class AuditoriaEventoConfiguration : IEntityTypeConfiguration<AuditoriaEvento>
{
    public void Configure(EntityTypeBuilder<AuditoriaEvento> builder)
    {
        builder.ToTable("auditoria_eventos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(a => a.Accion)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("accion");

        builder.Property(a => a.EntidadNombre)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("entidad_nombre");

        builder.Property(a => a.EntidadId)
            .HasMaxLength(100)
            .HasColumnName("entidad_id");

        builder.Property(a => a.ValoresAnterioresJSON)
            .HasColumnName("valores_anteriores_json");

        builder.Property(a => a.ValoresNuevosJSON)
            .HasColumnName("valores_nuevos_json");

        builder.Property(a => a.DireccionIp)
            .HasMaxLength(45)
            .HasColumnName("direccion_ip");

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .HasColumnName("user_agent");

        builder.Property(a => a.CorrelationId)
            .IsRequired()
            .HasColumnName("correlation_id");

        builder.Property(a => a.FechaEvento)
            .IsRequired()
            .HasColumnName("fecha_evento");

        builder.Property(a => a.FechaCreacion)
            .IsRequired()
            .HasColumnName("fecha_creacion");

        builder.Property(a => a.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(a => a.EsActivo)
            .IsRequired()
            .HasColumnName("es_activo");

        // Indexes
        builder.HasIndex(a => a.CorrelationId);

        builder.HasIndex(a => a.UsuarioId);

        builder.HasIndex(a => a.FechaEvento);

        // Relationships
        builder.HasOne(a => a.Usuario)
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
