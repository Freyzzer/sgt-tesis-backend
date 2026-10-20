using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.Auditoria;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Auditoria;

/// <summary>Acciones registradas en la auditoría (prefijo = área).</summary>
public static class AccionesAuditoria
{
    public const string Login = "Auth.Login";
    public const string LoginFallido = "Auth.LoginFallido";
    public const string LoginRechazado = "Auth.LoginRechazado";
    public const string CuentaBloqueada = "Auth.CuentaBloqueada";
    public const string RefreshReutilizado = "Auth.RefreshReutilizado";
    public const string Logout = "Auth.Logout";
    public const string PasswordCambiada = "Auth.PasswordCambiada";
    public const string PasswordRestablecimientoSolicitado = "Auth.PasswordRestablecimientoSolicitado";
    public const string PasswordRestablecida = "Auth.PasswordRestablecida";
    public const string UsuarioCreado = "Usuario.Creado";
    public const string UsuarioActivado = "Usuario.Activado";
    public const string UsuarioDesactivado = "Usuario.Desactivado";
    public const string UsuarioRolCambiado = "Usuario.RolCambiado";
    public const string AdminInicialCreado = "Usuario.AdminInicialCreado";
    public const string TesisRegistrada = "Tesis.Registrada";
    public const string EtapaEnviada = "Etapa.Enviada";
    public const string DocumentoCargado = "Documento.Cargado";
    public const string EvaluadorAsignado = "Evaluador.Asignado";
    public const string EvaluadorRetirado = "Evaluador.Retirado";
    public const string VeredictoEmitido = "Veredicto.Emitido";
    public const string DecisionRegistrada = "Decision.Registrada";
    public const string ReporteExportado = "Reporte.Exportado";
}

/// <summary>
/// Registra eventos en la misma transacción que el cambio de negocio: se guardan junto con el
/// siguiente <c>SaveChanges</c> del caso de uso.
/// </summary>
public interface IAuditTrail
{
    void Registrar(
        string accion,
        string entidad,
        Guid? entidadId = null,
        Guid? tesisId = null,
        string? detalle = null,
        Guid? actorId = null);
}

/// <summary>Datos de la solicitud en curso (IP y correlación) para la auditoría.</summary>
public interface IRequestContext
{
    string? Ip { get; }

    string? CorrelationId { get; }
}

public sealed record RegistroAuditoriaDto(
    Guid Id,
    Guid? ActorId,
    string Accion,
    string Entidad,
    Guid? EntidadId,
    Guid? TesisId,
    DateTime FechaUtc,
    string? Ip,
    string? CorrelacionId,
    string? Detalle)
{
    public static RegistroAuditoriaDto Desde(RegistroAuditoria r) =>
        new(r.Id, r.ActorId, r.Accion, r.Entidad, r.EntidadId, r.TesisId, r.FechaUtc, r.Ip, r.CorrelacionId, r.Detalle);
}

public interface IAuditoriaRepository
{
    Task<(IReadOnlyList<RegistroAuditoria> Items, int Total)> ListarAsync(
        ConsultarAuditoriaQuery filtro,
        CancellationToken cancellationToken);
}

public sealed record ConsultarAuditoriaQuery(
    Guid? TesisId = null,
    Guid? ActorId = null,
    string? Accion = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int Page = 1,
    int PageSize = 20);

public sealed class ConsultarAuditoriaQueryValidator : AbstractValidator<ConsultarAuditoriaQuery>
{
    public ConsultarAuditoriaQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListarUsuariosQueryValidator.PageSizeMaximo);
        RuleFor(x => x.Accion).MaximumLength(RegistroAuditoria.AccionMaxLength);
        RuleFor(x => x.Hasta).GreaterThanOrEqualTo(x => x.Desde)
            .When(x => x.Desde is not null && x.Hasta is not null)
            .WithMessage("'Hasta' no puede ser anterior a 'Desde'.");
    }
}

public sealed class ConsultarAuditoriaHandler(IAuditoriaRepository auditoria, ICurrentUser actual)
    : IQueryHandler<ConsultarAuditoriaQuery, PagedResult<RegistroAuditoriaDto>>
{
    public async Task<PagedResult<RegistroAuditoriaDto>> HandleAsync(ConsultarAuditoriaQuery query, CancellationToken cancellationToken)
    {
        if (actual.UserId is null || actual.Rol != nameof(Rol.Administrador))
        {
            throw new ProhibidoException();
        }

        var (items, total) = await auditoria.ListarAsync(query, cancellationToken);

        return new PagedResult<RegistroAuditoriaDto>(items.Select(RegistroAuditoriaDto.Desde).ToList(), query.Page, query.PageSize, total);
    }
}
