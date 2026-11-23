namespace SGT.Tesis.Domain.Common;

/// <summary>Violación de una regla o invariante de negocio.</summary>
public class DomainException(string message) : Exception(message);
