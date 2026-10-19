using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Application.Common.Abstractions;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Rol { get; }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>Consume un tiempo equivalente a una verificación real (evita enumerar correos por tiempo de respuesta).</summary>
    void SimularVerificacion(string password);
}

public sealed record AccessToken(string Valor, DateTime ExpiraEn);

public sealed record RefreshTokenGenerado(string Valor, string Hash);

public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    AccessToken CrearAccessToken(Usuario usuario);

    RefreshTokenGenerado GenerarRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancellationToken);

    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExisteCorreoAsync(string correoNormalizado, CancellationToken cancellationToken);

    Task<bool> ExisteAdministradorAsync(CancellationToken cancellationToken);

    Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Usuario>> ListarAdministradoresActivosAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(int page, int pageSize, Rol? rol, bool? activo, CancellationToken cancellationToken);

    void Agregar(Usuario usuario);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> ObtenerPorHashAsync(string hash, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ObtenerFamiliaAsync(Guid familiaId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ObtenerActivosDeUsuarioAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken);

    void Agregar(RefreshToken token);
}
