namespace SGT.Tesis.Domain.Common;

/// <summary>La operación no es válida para el estado actual del agregado (la API responde 409).</summary>
public sealed class TransicionInvalidaException(string message) : DomainException(message);
