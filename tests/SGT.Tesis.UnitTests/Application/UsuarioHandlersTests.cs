using Microsoft.Extensions.Logging.Abstractions;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Usuarios;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.UnitTests.Support;

namespace SGT.Tesis.UnitTests.Application;

public class UsuarioHandlersTests
{
    private sealed class Actual(Guid? id, string? rol) : ICurrentUser
    {
        public Guid? UserId { get; } = id;

        public string? Rol { get; } = rol;
    }

    private sealed class Reloj : IClock
    {
        public DateTime UtcNow => new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class Guardado : IUnitOfWork
    {
        public int Guardados { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }

    private sealed class Usuarios(params Usuario[] existentes) : IUsuarioRepository
    {
        public List<Usuario> Datos { get; } = [.. existentes];

        public Task<Usuario?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) =>
            Task.FromResult(Datos.FirstOrDefault(u => u.Correo == correoNormalizado));

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Datos.FirstOrDefault(u => u.Id == id));

        public Task<bool> ExisteCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) =>
            Task.FromResult(Datos.Any(u => u.Correo == correoNormalizado));

        public Task<bool> ExisteAdministradorAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Datos.Any(u => u.Rol == Rol.Administrador));

        public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Datos.Count(u => u.Rol == Rol.Administrador && u.Activo));

        public Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(int page, int pageSize, Rol? rol, bool? activo, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Usuario>)Datos, Datos.Count));

        public void Agregar(Usuario usuario) => Datos.Add(usuario);
    }

    private sealed class Tokens(params RefreshToken[] existentes) : IRefreshTokenRepository
    {
        public List<RefreshToken> Datos { get; } = [.. existentes];

        public Task<RefreshToken?> ObtenerPorHashAsync(string hash, CancellationToken cancellationToken) =>
            Task.FromResult(Datos.FirstOrDefault(t => t.TokenHash == hash));

        public Task<IReadOnlyList<RefreshToken>> ObtenerFamiliaAsync(Guid familiaId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<RefreshToken>)Datos.Where(t => t.FamiliaId == familiaId).ToList());

        public Task<IReadOnlyList<RefreshToken>> ObtenerActivosDeUsuarioAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<RefreshToken>)Datos.Where(t => t.UsuarioId == usuarioId && t.EstaActivo(utcNow)).ToList());

        public void Agregar(RefreshToken token) => Datos.Add(token);
    }

    private static Usuario Admin(string correo = "admin@x.edu") => Usuario.Crear("Ad", "Min", correo, "hash", Rol.Administrador);

    private static CambiarEstadoUsuarioHandler EstadoHandler(Usuarios usuarios, Tokens tokens, ICurrentUser actual, Guardado? guardado = null) =>
        new(usuarios, tokens, actual, new Reloj(), guardado ?? new Guardado(), new AuditoriaEnMemoria(), NullLogger<CambiarEstadoUsuarioHandler>.Instance);

    [Fact]
    [Trait("AC", "3.3")]
    public async Task No_se_puede_desactivar_al_ultimo_administrador_activo()
    {
        var ultimo = Admin();
        var actor = Usuario.Crear("Otro", "Actor", "actor@x.edu", "hash", Rol.Estudiante);
        var usuarios = new Usuarios(ultimo, actor);
        var handler = EstadoHandler(usuarios, new Tokens(), new Actual(Guid.NewGuid(), nameof(Rol.Administrador)));

        await Assert.ThrowsAsync<ConflictoException>(
            () => handler.HandleAsync(new CambiarEstadoUsuarioCommand(ultimo.Id, false), CancellationToken.None));
        Assert.True(ultimo.Activo);
    }

    [Fact]
    [Trait("AC", "3.3")]
    public async Task Desactivar_revoca_los_refresh_tokens_vigentes_del_usuario()
    {
        var objetivo = Usuario.Crear("Ana", "Pérez", "ana@x.edu", "hash", Rol.Estudiante);
        var vigente = RefreshToken.Crear(objetivo.Id, "h1", new Reloj().UtcNow.AddDays(3));
        var otroUsuario = RefreshToken.Crear(Guid.NewGuid(), "h2", new Reloj().UtcNow.AddDays(3));
        var guardado = new Guardado();
        var handler = EstadoHandler(new Usuarios(objetivo), new Tokens(vigente, otroUsuario), new Actual(Guid.NewGuid(), nameof(Rol.Administrador)), guardado);

        await handler.HandleAsync(new CambiarEstadoUsuarioCommand(objetivo.Id, false), CancellationToken.None);

        Assert.False(objetivo.Activo);
        Assert.True(vigente.EstaRevocado);
        Assert.False(otroUsuario.EstaRevocado);
        Assert.Equal(1, guardado.Guardados);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Sin_rol_de_administrador_el_caso_de_uso_lanza_Prohibido_aunque_se_omita_la_politica_del_endpoint()
    {
        var objetivo = Usuario.Crear("Ana", "Pérez", "ana@x.edu", "hash", Rol.Estudiante);
        var handler = EstadoHandler(new Usuarios(objetivo), new Tokens(), new Actual(Guid.NewGuid(), nameof(Rol.Estudiante)));

        await Assert.ThrowsAsync<ProhibidoException>(
            () => handler.HandleAsync(new CambiarEstadoUsuarioCommand(objetivo.Id, false), CancellationToken.None));
        Assert.True(objetivo.Activo);
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task Sin_usuario_autenticado_el_caso_de_uso_lanza_Prohibido()
    {
        var handler = new ListarUsuariosHandler(new Usuarios(), new Actual(null, null));

        await Assert.ThrowsAsync<ProhibidoException>(
            () => handler.HandleAsync(new ListarUsuariosQuery(), CancellationToken.None));
    }

    [Fact]
    [Trait("AC", "3.1")]
    public async Task No_se_puede_quitar_el_rol_al_ultimo_administrador_activo()
    {
        var ultimo = Admin();
        var usuarios = new Usuarios(ultimo);
        var handler = new CambiarRolUsuarioHandler(
            usuarios,
            new Actual(Guid.NewGuid(), nameof(Rol.Administrador)),
            new Guardado(),
            new AuditoriaEnMemoria(),
            NullLogger<CambiarRolUsuarioHandler>.Instance);

        await Assert.ThrowsAsync<ConflictoException>(
            () => handler.HandleAsync(new CambiarRolUsuarioCommand(ultimo.Id, Rol.Estudiante), CancellationToken.None));
        Assert.Equal(Rol.Administrador, ultimo.Rol);
    }
}
