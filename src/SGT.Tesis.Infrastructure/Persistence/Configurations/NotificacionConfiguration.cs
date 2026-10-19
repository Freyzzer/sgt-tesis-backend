using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGT.Tesis.Domain.Notificaciones;

namespace SGT.Tesis.Infrastructure.Persistence.Configurations;

internal sealed class NotificacionConfiguration : IEntityTypeConfiguration<NotificacionPendiente>
{
    public void Configure(EntityTypeBuilder<NotificacionPendiente> builder)
    {
        builder.ToTable("Notificaciones");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Destinatario).IsRequired().HasMaxLength(NotificacionPendiente.DestinatarioMaxLength);
        builder.Property(n => n.Asunto).IsRequired().HasMaxLength(NotificacionPendiente.AsuntoMaxLength);
        builder.Property(n => n.Cuerpo).IsRequired().HasMaxLength(NotificacionPendiente.CuerpoMaxLength);
        builder.Property(n => n.Estado).IsRequired().HasConversion<int>();
        builder.Property(n => n.UltimoError).HasMaxLength(NotificacionPendiente.ErrorMaxLength);

        builder.HasIndex(n => new { n.Estado, n.ProximoIntentoEn });
    }
}
