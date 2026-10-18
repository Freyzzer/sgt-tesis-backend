using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Usuarios;

public sealed record ListarUsuariosQuery(int Page = 1, int PageSize = 20, Rol? Rol = null, bool? Activo = null);

public sealed class ListarUsuariosQueryValidator : AbstractValidator<ListarUsuariosQuery>
{
    public const int PageSizeMaximo = 100;

    public ListarUsuariosQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageSizeMaximo);
        RuleFor(x => x.Rol).IsInEnum().When(x => x.Rol is not null);
    }
}

public sealed class ListarUsuariosHandler(IUsuarioRepository usuarios, ICurrentUser actual)
    : IQueryHandler<ListarUsuariosQuery, PagedResult<UsuarioDto>>
{
    public async Task<PagedResult<UsuarioDto>> HandleAsync(ListarUsuariosQuery query, CancellationToken cancellationToken)
    {
        AutorizacionAdministrador.Exigir(actual);

        var (items, total) = await usuarios.ListarAsync(query.Page, query.PageSize, query.Rol, query.Activo, cancellationToken);

        return new PagedResult<UsuarioDto>(items.Select(UsuarioDto.Desde).ToList(), query.Page, query.PageSize, total);
    }
}

public sealed record ObtenerUsuarioQuery(Guid Id);

public sealed class ObtenerUsuarioHandler(IUsuarioRepository usuarios, ICurrentUser actual)
    : IQueryHandler<ObtenerUsuarioQuery, UsuarioDto>
{
    public async Task<UsuarioDto> HandleAsync(ObtenerUsuarioQuery query, CancellationToken cancellationToken)
    {
        AutorizacionAdministrador.Exigir(actual);

        var usuario = await usuarios.ObtenerPorIdAsync(query.Id, cancellationToken)
            ?? throw new NoEncontradoException("El usuario no existe.");

        return UsuarioDto.Desde(usuario);
    }
}
