using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.GestionTesis;

public sealed record EtapaDto(Guid Id, string Tipo, int Numero, string Estado, DateTime FechaApertura, DateTime? FechaCierre)
{
    public static EtapaDto Desde(Etapa e) => new(e.Id, e.Tipo.ToString(), (int)e.Tipo, e.Estado.ToString(), e.FechaApertura, e.FechaCierre);
}

public sealed record TesisDto(
    Guid Id,
    Guid StudentId,
    string Titulo,
    string Resumen,
    string Estado,
    EtapaDto EtapaActual,
    IReadOnlyList<EtapaDto> Etapas)
{
    public static TesisDto Desde(TesisGrado t) => new(
        t.Id,
        t.StudentId,
        t.Titulo,
        t.Resumen,
        t.Estado.ToString(),
        EtapaDto.Desde(t.EtapaActual),
        t.Etapas.OrderBy(e => e.Tipo).Select(EtapaDto.Desde).ToList());
}

/// <summary>Requisitos documentales de cada etapa (se completa con la carga de documentos, US-07).</summary>
public interface IRequisitosDocumentales
{
    /// <summary>Nombres de los documentos obligatorios que aún no se han cargado para la etapa.</summary>
    Task<IReadOnlyList<string>> ObtenerFaltantesAsync(Guid etapaId, EtapaTipo tipo, CancellationToken cancellationToken);
}

public interface ITesisRepository
{
    Task<TesisGrado?> ObtenerActivaDeEstudianteAsync(Guid studentId, CancellationToken cancellationToken);

    Task<TesisGrado?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExisteActivaAsync(Guid studentId, CancellationToken cancellationToken);

    void Agregar(TesisGrado tesis);
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
}
