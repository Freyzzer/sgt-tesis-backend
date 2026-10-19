using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Consultas;

// ---------- Perfil propio ----------

public sealed record PerfilDto(Guid Id, string Nombres, string Apellidos, string Correo, string Rol, bool DebeCambiarPassword);

public sealed record ObtenerPerfilQuery;

/// <summary>Datos de la persona autenticada; el token no los incluye a propósito (solo identificador y rol).</summary>
public sealed class ObtenerPerfilHandler(IUsuarioRepository usuarios, ICurrentUser actual)
    : IQueryHandler<ObtenerPerfilQuery, PerfilDto>
{
    public async Task<PerfilDto> HandleAsync(ObtenerPerfilQuery query, CancellationToken cancellationToken)
    {
        var id = actual.UserId ?? throw new NoAutenticadoException();
        var u = await usuarios.ObtenerPorIdAsync(id, cancellationToken) ?? throw new NoAutenticadoException();

        return new PerfilDto(u.Id, u.Nombres, u.Apellidos, u.Correo, u.Rol.ToString(), u.DebeCambiarPassword);
    }
}

// ---------- Mis evaluaciones (Evaluador) ----------

public sealed record EvaluacionAsignadaDto(
    Guid TesisId,
    string Titulo,
    string Estudiante,
    int Etapa,
    string TipoEtapa,
    string EstadoEtapa,
    DateTime AsignadaEn,
    DateTime? EnRevisionDesde,
    bool VeredictoEmitido);

public sealed record ListarMisEvaluacionesQuery(bool SoloPendientes = false, int Page = 1, int PageSize = 20);

public sealed class ListarMisEvaluacionesQueryValidator : AbstractValidator<ListarMisEvaluacionesQuery>
{
    public ListarMisEvaluacionesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListarUsuariosQueryValidator.PageSizeMaximo);
    }
}

public sealed class ListarMisEvaluacionesHandler(IConsultasRepository consultas, ICurrentUser actual)
    : IQueryHandler<ListarMisEvaluacionesQuery, PagedResult<EvaluacionAsignadaDto>>
{
    public async Task<PagedResult<EvaluacionAsignadaDto>> HandleAsync(ListarMisEvaluacionesQuery query, CancellationToken cancellationToken)
    {
        if (actual.UserId is not { } evaluadorId || actual.Rol != nameof(Rol.Evaluador))
        {
            throw new ProhibidoException("Solo un Evaluador tiene evaluaciones asignadas.");
        }

        var (items, total) = await consultas.ListarEvaluacionesAsignadasAsync(evaluadorId, query.SoloPendientes, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<EvaluacionAsignadaDto>(items, query.Page, query.PageSize, total);
    }
}

// ---------- Listado de tesis (Administrador) ----------

public sealed record TesisResumenDto(
    Guid Id,
    string Titulo,
    Guid EstudianteId,
    string Estudiante,
    string Estado,
    int EtapaActual,
    string TipoEtapaActual,
    string EstadoEtapaActual,
    DateTime CreatedAt);

public sealed record ListarTesisQuery(EstadoTesis? Estado = null, int? Etapa = null, string? Texto = null, int Page = 1, int PageSize = 20);

public sealed class ListarTesisQueryValidator : AbstractValidator<ListarTesisQuery>
{
    public ListarTesisQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListarUsuariosQueryValidator.PageSizeMaximo);
        RuleFor(x => x.Estado).IsInEnum().When(x => x.Estado is not null);
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n!.Value)).When(x => x.Etapa is not null).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.Texto).MaximumLength(100);
    }
}

public sealed class ListarTesisHandler(IConsultasRepository consultas, ICurrentUser actual)
    : IQueryHandler<ListarTesisQuery, PagedResult<TesisResumenDto>>
{
    public async Task<PagedResult<TesisResumenDto>> HandleAsync(ListarTesisQuery query, CancellationToken cancellationToken)
    {
        if (actual.UserId is null || actual.Rol != nameof(Rol.Administrador))
        {
            throw new ProhibidoException();
        }

        var (items, total) = await consultas.ListarTesisAsync(query, cancellationToken);

        return new PagedResult<TesisResumenDto>(items, query.Page, query.PageSize, total);
    }
}

public interface IConsultasRepository
{
    Task<(IReadOnlyList<EvaluacionAsignadaDto> Items, int Total)> ListarEvaluacionesAsignadasAsync(
        Guid evaluadorId,
        bool soloPendientes,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<TesisResumenDto> Items, int Total)> ListarTesisAsync(ListarTesisQuery filtro, CancellationToken cancellationToken);
}
