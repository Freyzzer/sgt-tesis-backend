using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Auditoria;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Accion).IsRequired().HasMaxLength(RegistroAuditoria.AccionMaxLength);
        builder.Property(r => r.Entidad).IsRequired().HasMaxLength(RegistroAuditoria.EntidadMaxLength);
        builder.Property(r => r.Ip).HasMaxLength(RegistroAuditoria.IpMaxLength);
        builder.Property(r => r.CorrelacionId).HasMaxLength(RegistroAuditoria.CorrelacionMaxLength);
        builder.Property(r => r.Detalle).HasMaxLength(RegistroAuditoria.DetalleMaxLength);
        builder.Property(r => r.FechaUtc).IsRequired();

        builder.HasIndex(r => r.FechaUtc);
        builder.HasIndex(r => new { r.TesisId, r.FechaUtc });
        builder.HasIndex(r => new { r.ActorId, r.FechaUtc });
        builder.HasIndex(r => r.Accion);
    }
}
