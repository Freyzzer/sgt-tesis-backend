namespace SGT.Tesis.Domain.Common;

/// <summary>
/// Clase base genérica para las entidades de dominio con identificador tipado.
/// </summary>
/// <typeparam name="TKey">Tipo de la clave primaria (int, Guid, etc.).</typeparam>
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public bool EsActivo { get; set; } = true;
}

/// <summary>
/// Clase base por defecto que utiliza int como clave primaria.
/// </summary>
public abstract class BaseEntity : BaseEntity<int>
{
}
