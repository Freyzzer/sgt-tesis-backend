using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.GestionTesis;

public sealed record EtapaDto(Guid Id, string Tipo, int Numero, string Estado, DateTime FechaApertura, DateTime? FechaCierre)
{
    public static EtapaDto Desde(Etapa e) => new(e.Id, e.Tipo.ToString(), (int)e.Tipo, e.Estado.ToString(), e.FechaApertura, e.FechaCierre);
}

public sealed record DecisionDto(Guid Id, int Etapa, string Resultado, string? Justificacion, Guid AdministradorId, DateTime FechaUtc)
{
    public static DecisionDto Desde(Decision d, TesisGrado tesis) => new(
        d.Id,
        (int)tesis.Etapas.First(e => e.Id == d.EtapaId).Tipo,
        d.Resultado.ToString(),
        d.Justificacion,
        d.AdministradorId,
        d.FechaUtc);
}

public sealed record TesisDto(
    Guid Id,
    Guid StudentId,
    string Titulo,
    string Resumen,
    string Estado,
    EtapaDto EtapaActual,
    IReadOnlyList<EtapaDto> Etapas,
    IReadOnlyList<DecisionDto> Decisiones)
{
    public static TesisDto Desde(TesisGrado t, IEnumerable<Decision>? decisiones = null) => new(
        t.Id,
        t.StudentId,
        t.Titulo,
        t.Resumen,
        t.Estado.ToString(),
        EtapaDto.Desde(t.EtapaActual),
        t.Etapas.OrderBy(e => e.Tipo).Select(EtapaDto.Desde).ToList(),
        (decisiones ?? []).OrderBy(d => d.FechaUtc).Select(d => DecisionDto.Desde(d, t)).ToList());
}

/// <summary>Reglas configurables de cada etapa (ver <c>Flujo:Etapas</c> y ADR-005).</summary>
public interface IPoliticasDeEtapa
{
    PoliticaEtapa Para(EtapaTipo tipo);
}

/// <summary>Requisitos documentales de cada etapa (ver <c>Documentos:Requisitos</c>).</summary>
public interface IRequisitosDocumentales
{
    /// <summary>Nombres de los documentos obligatorios que aún no se han cargado para la etapa.</summary>
    Task<IReadOnlyList<string>> ObtenerFaltantesAsync(Guid etapaId, EtapaTipo tipo, CancellationToken cancellationToken);
}

public interface ITesisRepository
{
    Task<TesisGrado?> ObtenerActivaDeEstudianteAsync(Guid studentId, CancellationToken cancellationToken);

    Task<TesisGrado?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TesisGrado?> ObtenerPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken);

    Task<bool> ExisteActivaAsync(Guid studentId, CancellationToken cancellationToken);

    void Agregar(TesisGrado tesis);
}

public interface IDecisionRepository
{
    Task<IReadOnlyList<Decision>> ListarPorTesisAsync(Guid tesisId, CancellationToken cancellationToken);

    void Agregar(Decision decision);
}

internal static class AutorizacionTesis
{
    public static Guid ExigirEstudiante(ICurrentUser actual)
    {
        if (actual.UserId is not { } id || actual.Rol != nameof(Rol.Estudiante))
        {
            throw new ProhibidoException("Solo un Estudiante puede realizar esta acción.");
        }

        return id;
    }

    public static Guid ExigirAdministrador(ICurrentUser actual)
    {
        if (actual.UserId is not { } id || actual.Rol != nameof(Rol.Administrador))
        {
            throw new ProhibidoException("Solo un Administrador puede realizar esta acción.");
        }

        return id;
    }

    /// <summary>El Estudiante solo ve su tesis y el Administrador todas; para el resto la tesis "no existe".</summary>
    public static void ExigirVisible(TesisGrado tesis, ICurrentUser actual)
    {
        var esDueno = actual.Rol == nameof(Rol.Estudiante) && actual.UserId == tesis.StudentId;
        var esAdministrador = actual.Rol == nameof(Rol.Administrador);

        if (!esDueno && !esAdministrador)
        {
            throw new NoEncontradoException("La tesis no existe.");
        }
    }

    /// <summary>Como <see cref="ExigirVisible"/>, y además el Evaluador con una asignación activa en la tesis.</summary>
    public static async Task ExigirVisibleAsync(
        TesisGrado tesis,
        ICurrentUser actual,
        IAsignacionRepository asignaciones,
        CancellationToken cancellationToken)
    {
        if (actual.Rol == nameof(Rol.Evaluador) && actual.UserId is { } evaluadorId
            && await asignaciones.TieneAsignacionActivaEnTesisAsync(evaluadorId, tesis.Id, cancellationToken))
        {
            return;
        }

        ExigirVisible(tesis, actual);
    }
}
