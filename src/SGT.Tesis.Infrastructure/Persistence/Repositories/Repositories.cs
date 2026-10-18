using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.Infrastructure.Persistence.Repositories;

internal sealed class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correoNormalizado, cancellationToken);

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> ExisteCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) =>
        db.Usuarios.AnyAsync(u => u.Correo == correoNormalizado, cancellationToken);

    public Task<bool> ExisteAdministradorAsync(CancellationToken cancellationToken) =>
        db.Usuarios.AnyAsync(u => u.Rol == Rol.Administrador, cancellationToken);

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);
}

internal sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> ObtenerPorHashAsync(string hash, CancellationToken cancellationToken) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ObtenerFamiliaAsync(Guid familiaId, CancellationToken cancellationToken) =>
        await db.RefreshTokens.Where(t => t.FamiliaId == familiaId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ObtenerActivosDeUsuarioAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevocadoEn == null && t.ExpiraEn > utcNow)
            .ToListAsync(cancellationToken);

    public void Agregar(RefreshToken token) => db.RefreshTokens.Add(token);
}
