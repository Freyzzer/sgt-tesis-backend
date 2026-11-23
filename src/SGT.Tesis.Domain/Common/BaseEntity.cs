namespace SGT.Tesis.Domain.Common;

/// <summary>
/// Base de toda entidad persistida: identificador GUID no secuencial
/// (evita la enumeración de recursos) y campos de auditoría.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public void MarkCreated(DateTime utcNow, Guid? userId)
    {
        CreatedAt = utcNow;
        CreatedBy = userId;
    }

    public void MarkUpdated(DateTime utcNow, Guid? userId)
    {
        UpdatedAt = utcNow;
        UpdatedBy = userId;
    }
}
