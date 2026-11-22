using FluentValidation;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Usuarios;

public sealed record UsuarioDto(
    Guid Id,
    string Nombres,
    string Apellidos,
    string Correo,
    string Rol,
    bool Activo,
    DateTime CreatedAt)
{
    public static UsuarioDto Desde(Usuario u) => new(u.Id, u.Nombres, u.Apellidos, u.Correo, u.Rol.ToString(), u.Activo, u.CreatedAt);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

internal static class AutorizacionAdministrador
{
    /// <summary>La autorización se verifica en el caso de uso, además de la política del endpoint.</summary>
    public static void Exigir(ICurrentUser actual)
    {
        if (actual.UserId is null || actual.Rol != nameof(Rol.Administrador))
        {
            throw new ProhibidoException();
        }
    }
}

internal static class PoliticaDeContrasena
{
    public const int Minimo = 12;

    public static IRuleBuilderOptions<T, string> CumplePolitica<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(Minimo).WithMessage($"La contraseña debe tener al menos {Minimo} caracteres.")
        .MaximumLength(128)
        .Matches("[A-Z]").WithMessage("La contraseña debe incluir una mayúscula.")
        .Matches("[a-z]").WithMessage("La contraseña debe incluir una minúscula.")
        .Matches("[0-9]").WithMessage("La contraseña debe incluir un número.")
        .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe incluir un símbolo.");
}
