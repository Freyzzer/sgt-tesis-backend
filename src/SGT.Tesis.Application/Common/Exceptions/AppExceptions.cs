namespace SGT.Tesis.Application.Common.Exceptions;

/// <summary>Error esperado de un caso de uso; la API lo traduce a un código HTTP.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Credenciales o token inválidos (401). El mensaje es deliberadamente genérico.</summary>
public sealed class NoAutenticadoException(string message = "Credenciales inválidas.") : AppException(message);

/// <summary>El usuario autenticado no tiene permiso (403).</summary>
public sealed class ProhibidoException(string message = "No tiene permiso para realizar esta acción.") : AppException(message);

/// <summary>Recurso inexistente o no visible para el usuario (404).</summary>
public sealed class NoEncontradoException(string message = "Recurso no encontrado.") : AppException(message);

/// <summary>Conflicto con el estado actual (409).</summary>
public sealed class ConflictoException(string message) : AppException(message);

/// <summary>Datos de entrada inválidos (400) con el detalle por campo.</summary>
public sealed class ValidacionException(IDictionary<string, string[]> errores) : AppException("Uno o más campos son inválidos.")
{
    public IDictionary<string, string[]> Errores { get; } = errores;
}
