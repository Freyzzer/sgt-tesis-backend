using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Notificaciones;

/// <summary>Encola un correo para enviarlo después, en la misma transacción que el cambio de negocio.</summary>
public interface INotificador
{
    void Encolar(string destinatario, string asunto, string cuerpo);
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

    public static (string Asunto, string Cuerpo) Decision(EtapaTipo tipo, ResultadoDecision resultado) => resultado switch
    {
        ResultadoDecision.Aprobada => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} fue aprobada",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue aprobada.\n\nIngrese al sistema para continuar con la siguiente etapa."),
        ResultadoDecision.Observada => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} tiene observaciones",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue observada.\n\nIngrese al sistema para revisar la retroalimentación, hacer las correcciones y reenviarla."),
        _ => (
            $"SGT-Tesis: su etapa {NombreEtapa(tipo)} fue rechazada",
            $"La etapa {NombreEtapa(tipo)} de su tesis fue rechazada.\n\nIngrese al sistema para consultar el detalle."),
    };
}
