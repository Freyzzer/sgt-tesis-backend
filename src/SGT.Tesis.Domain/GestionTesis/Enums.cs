namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>Las 7 etapas del proceso, en orden.</summary>
public enum EtapaTipo
{
    Estudiante = 1,
    Propuesta = 2,
    Consejo = 3,
    Evaluacion = 4,
    Pasantia = 5,
    Articulo = 6,
    RequisitosFinales = 7,
}

public enum EstadoEtapa
{
    Pendiente = 1,
    EnRevision = 2,
    Observado = 3,
    Aprobado = 4,
    Rechazado = 5,
}

public enum EstadoTesis
{
    EnCurso = 1,
    Finalizada = 2,
    Rechazada = 3,
}
