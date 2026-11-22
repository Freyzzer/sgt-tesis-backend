using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Notificaciones;

public enum EstadoNotificacion
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3,
}

/// <summary>
/// Correo por enviar (patrón outbox). Se guarda en la misma transacción que el cambio de negocio y un
/// proceso aparte lo envía con reintentos; un fallo de correo nunca revierte la operación original.
/// </summary>
public sealed class NotificacionPendiente : BaseEntity
{
    public const int DestinatarioMaxLength = 254;
    public const int AsuntoMaxLength = 200;
    public const int CuerpoMaxLength = 4000;
    public const int ErrorMaxLength = 500;

    private NotificacionPendiente()
    {
        Destinatario = string.Empty;
        Asunto = string.Empty;
        Cuerpo = string.Empty;
    }

    public string Destinatario { get; private set; }

    public string Asunto { get; private set; }

    public string Cuerpo { get; private set; }

    public EstadoNotificacion Estado { get; private set; }

    public int Intentos { get; private set; }

    public DateTime ProximoIntentoEn { get; private set; }

    public DateTime? EnviadaEn { get; private set; }

    public string? UltimoError { get; private set; }

    /// <summary>El cuerpo contiene un secreto: se borra al enviarse o al agotar los intentos.</summary>
    public bool Sensible { get; private set; }

    public const string CuerpoDepurado = "[Contenido sensible eliminado]";

    public static NotificacionPendiente Crear(string? destinatario, string? asunto, string? cuerpo, DateTime utcNow, bool sensible = false)
    {
        var correo = Guard.Required(destinatario, DestinatarioMaxLength, nameof(Destinatario));

        if (!correo.Contains('@'))
        {
            throw new DomainException("El destinatario no es un correo válido.");
        }

        return new NotificacionPendiente
        {
            Destinatario = correo,
            Asunto = Guard.Required(asunto, AsuntoMaxLength, nameof(Asunto)),
            Cuerpo = Guard.Required(cuerpo, CuerpoMaxLength, nameof(Cuerpo)),
            Estado = EstadoNotificacion.Pendiente,
            ProximoIntentoEn = utcNow,
            Sensible = sensible,
        };
    }

    public bool DebeEnviarse(DateTime utcNow) => Estado == EstadoNotificacion.Pendiente && ProximoIntentoEn <= utcNow;

    public void MarcarEnviada(DateTime utcNow)
    {
        Estado = EstadoNotificacion.Enviada;
        EnviadaEn = utcNow;
        UltimoError = null;
        Depurar();
    }

    private void Depurar()
    {
        if (Sensible)
        {
            Cuerpo = CuerpoDepurado;
        }
    }

    /// <summary>Registra un fallo y programa el reintento con espera exponencial; tras agotar los intentos queda Fallida.</summary>
    public void RegistrarFallo(string? error, DateTime utcNow, int maxIntentos, TimeSpan esperaBase)
    {
        Intentos++;
        var texto = error?.Trim() ?? string.Empty;
        UltimoError = texto.Length > ErrorMaxLength ? texto[..ErrorMaxLength] : texto;

        if (Intentos >= maxIntentos)
        {
            Estado = EstadoNotificacion.Fallida;
            Depurar();
            return;
        }

        ProximoIntentoEn = utcNow + esperaBase * Math.Pow(2, Intentos - 1);
    }
}
