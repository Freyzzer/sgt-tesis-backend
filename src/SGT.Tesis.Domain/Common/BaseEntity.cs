namespace SGT.Tesis.Domain.Common;

/// <summary>
/// Generic base class for domain entities with a strongly typed identifier.
/// </summary>
/// <typeparam name="TKey">The type of the primary key (e.g., int, Guid).</typeparam>
public abstract class BaseEntity<TKey>
{
    /// <summary>
    /// Gets or sets the entity unique identifier.
    /// </summary>
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified, if applicable.
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is active.
    /// </summary>
    public bool EsActivo { get; set; } = true;
}

/// <summary>
/// Default base class for domain entities using <see cref="int"/> as the primary key type.
/// </summary>
public abstract class BaseEntity : BaseEntity<int>
{
}
