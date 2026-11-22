using SGT.Tesis.Application.Auditoria;

namespace SGT.Tesis.UnitTests.Support;

/// <summary>Doble de <see cref="IAuditTrail"/> que conserva los eventos registrados para poder verificarlos.</summary>
public sealed class AuditoriaEnMemoria : IAuditTrail
{
    public sealed record Evento(string Accion, string Entidad, Guid? EntidadId, Guid? TesisId, string? Detalle, Guid? ActorId);

    public List<Evento> Eventos { get; } = [];

    public void Registrar(string accion, string entidad, Guid? entidadId = null, Guid? tesisId = null, string? detalle = null, Guid? actorId = null) =>
        Eventos.Add(new Evento(accion, entidad, entidadId, tesisId, detalle, actorId));
}
