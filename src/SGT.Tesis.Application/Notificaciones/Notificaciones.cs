using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Notificaciones;

/// <summary>Encola un correo para enviarlo después, en la misma transacción que el cambio de negocio.</summary>
public interface INotificador
{
    /// <param name="sensible">
    /// El cuerpo contiene un secreto (por ejemplo, un token de restablecimiento): se borra de la base de datos
    /// en cuanto el correo se envía o se agotan los intentos.
    /// </param>
    void Encolar(string destinatario, string asunto, string cuerpo, bool sensible = false);
}

/// <summary>Direcciones del frontend usadas en los correos (configurables por ambiente).</summary>
public interface IAppLinks
{
    /// <summary>Plantilla con la marca <c>{token}</c>; <c>null</c> si no está configurada.</summary>
    string? UrlRestablecerPassword { get; }
}

/// <summary>
/// Plantillas de correo. Por privacidad solo mencionan la etapa y el tipo de evento: nunca el título de la
/// tesis, nombres de personas ni el texto de justificaciones o veredictos (se consultan en el sistema).
/// </summary>
public static class Plantillas
{
    public static string NombreEtapa(EtapaTipo tipo) => tipo switch
    {
        EtapaTipo.Estudiante => "Estudiante",
        EtapaTipo.Propuesta => "Propuesta",
        EtapaTipo.Consejo => "Consejo",
        EtapaTipo.Evaluacion => "Evaluación",
        EtapaTipo.Pasantia => "Pasantía",
        EtapaTipo.Articulo => "Artículo",
        EtapaTipo.RequisitosFinales => "Requisitos finales",
        _ => tipo.ToString(),
    };

    public static (string Asunto, string Cuerpo) EtapaEnRevision(EtapaTipo tipo) => (
        "SGT-Tesis: nueva etapa pendiente de validación",
        $"La etapa {NombreEtapa(tipo)} de una tesis fue enviada a revisión y espera su validación.\n\nIngrese al sistema para revisarla.");

    public static (string Asunto, string Cuerpo) EvaluadorAsignado(EtapaTipo tipo) => (
        "SGT-Tesis: nueva asignación de evaluación",
        $"Se le asignó la evaluación de la etapa {NombreEtapa(tipo)} de una tesis.\n\nIngrese al sistema para ver el detalle y emitir su veredicto.");

    /// <summary>Aviso al estudiante: un evaluador emitió o actualizó su veredicto. No incluye el concepto ni los comentarios (se consultan en el sistema).</summary>
    public static (string Asunto, string Cuerpo) VeredictoEmitido(EtapaTipo tipo, int ronda) => (
        $"SGT-Tesis: nueva evaluación de su etapa {NombreEtapa(tipo)}",
        $"Un evaluador emitió su veredicto sobre la etapa {NombreEtapa(tipo)} de su tesis (ronda {ronda}).\n\nIngrese al sistema para ver su avance y la retroalimentación.");

    /// <summary>Aviso al evaluador: la etapa que tiene asignada entró en una ronda de revisión (el estudiante la envió o la reenvió).</summary>
    public static (string Asunto, string Cuerpo) EtapaParaEvaluar(EtapaTipo tipo, int ronda) => (
        $"SGT-Tesis: etapa {NombreEtapa(tipo)} lista para evaluar",
        ronda > 1
            ? $"La etapa {NombreEtapa(tipo)} que tiene asignada fue corregida y reenviada (ronda {ronda}).\n\nIngrese al sistema para volver a evaluarla y emitir su veredicto."
            : $"La etapa {NombreEtapa(tipo)} que tiene asignada fue enviada a revisión.\n\nIngrese al sistema para evaluarla y emitir su veredicto."
    );

    /// <summary>Correo con el enlace (o el código, si no hay URL configurada) para restablecer la contraseña.</summary>
    public static (string Asunto, string Cuerpo) RestablecerPassword(string? plantillaUrl, string token)
    {
        var acceso = string.IsNullOrWhiteSpace(plantillaUrl)
            ? $"Use este código en la pantalla de restablecimiento:\n\n{token}"
            : $"Abra este enlace:\n\n{plantillaUrl.Replace("{token}", Uri.EscapeDataString(token))}";

        return (
            "SGT-Tesis: restablecimiento de contraseña",
            $"Recibimos una solicitud para restablecer la contraseña de su cuenta.\n\n{acceso}\n\nEl acceso vence en 30 minutos y solo puede usarse una vez. Si usted no lo solicitó, ignore este mensaje: su contraseña no cambiará.");
    }

    public static (string Asunto, string Cuerpo) PasswordCambiada() => (
        "SGT-Tesis: su contraseña fue cambiada",
        "La contraseña de su cuenta fue cambiada y se cerraron sus sesiones abiertas.\n\nSi usted no realizó este cambio, comuníquese de inmediato con el administrador del sistema.");

    public static (string Asunto, string Cuerpo) Decision(EtapaTipo tipo, ResultadoDecision resultado, bool reintentable = false) => resultado switch
    {
        ResultadoDecision.Aprobada => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} fue aprobada",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue aprobada.\n\nIngrese al sistema para continuar con la siguiente etapa."),
        ResultadoDecision.Observada => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} tiene observaciones",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue observada.\n\nIngrese al sistema para revisar la retroalimentación, hacer las correcciones y reenviarla."),
        ResultadoDecision.Rechazada when reintentable => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} fue rechazada",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue rechazada, pero puede corregirla y volver a enviarla.\n\nIngrese al sistema para revisar la retroalimentación."),
        _ => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} fue rechazada",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue rechazada.\n\nIngrese al sistema para consultar el detalle."),
    };
}
