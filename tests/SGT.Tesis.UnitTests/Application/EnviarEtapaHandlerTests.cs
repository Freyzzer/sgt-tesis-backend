using Microsoft.Extensions.Logging.Abstractions;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.UnitTests.Support;

namespace SGT.Tesis.UnitTests.Application;

public class EnviarEtapaHandlerTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Actual(Guid? id, string? rol) : ICurrentUser
    {
        public Guid? UserId { get; } = id;

        public string? Rol { get; } = rol;
    }

    private sealed class Repositorio(TesisGrado tesis) : ITesisRepository
    {
        public Task<TesisGrado?> ObtenerActivaDeEstudianteAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult<TesisGrado?>(tesis.StudentId == studentId ? tesis : null);

        public Task<TesisGrado?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<TesisGrado?>(tesis.Id == id ? tesis : null);

        public Task<TesisGrado?> ObtenerPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
            Task.FromResult<TesisGrado?>(tesis.Etapas.Any(e => e.Id == etapaId) ? tesis : null);

        public Task<bool> ExisteActivaAsync(Guid studentId, CancellationToken cancellationToken) =>
            Task.FromResult(tesis.StudentId == studentId);

        public void Agregar(TesisGrado nueva)
        {
        }
    }

    private sealed class DecisionesVacias : IDecisionRepository
    {
        public Task<IReadOnlyList<Decision>> ListarPorTesisAsync(Guid tesisId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Decision>>([]);

        public void Agregar(Decision decision)
        {
        }
    }

    private sealed class SinAsignaciones : IAsignacionRepository
    {
        public Task<AsignacionEvaluador?> ObtenerActivaAsync(Guid etapaId, Guid evaluadorId, CancellationToken cancellationToken) =>
            Task.FromResult<AsignacionEvaluador?>(null);

        public Task<IReadOnlyList<AsignacionEvaluador>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AsignacionEvaluador>>([]);

        public Task<bool> TieneAsignacionActivaEnTesisAsync(Guid evaluadorId, Guid tesisId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Agregar(AsignacionEvaluador asignacion)
        {
        }
    }

    private sealed class NotificadorEnMemoria : INotificador
    {
        public List<(string Destinatario, string Asunto, string Cuerpo)> Encolados { get; } = [];

        public void Encolar(string destinatario, string asunto, string cuerpo, bool sensible = false) => Encolados.Add((destinatario, asunto, cuerpo));
    }

    private sealed class Administradores : IUsuarioRepository
    {
        public Task<Usuario?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Usuario?>(null);

        public Task<bool> ExisteCorreoAsync(string correoNormalizado, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> ExisteAdministradorAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken) => Task.FromResult(1);

        public Task<IReadOnlyList<Usuario>> ListarAdministradoresActivosAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Usuario>>([Usuario.Crear("Ad", "Min", "admin@x.edu", "hash", Rol.Administrador)]);

        public Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAsync(int page, int pageSize, Rol? rol, bool? activo, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Usuario>)[], 0));

        public void Agregar(Usuario usuario)
        {
        }
    }

    private sealed class Reloj : IClock
    {
        public DateTime UtcNow => Ahora;
    }

    private sealed class Requisitos(params string[] faltantes) : IRequisitosDocumentales
    {
        public Task<IReadOnlyList<string>> ObtenerFaltantesAsync(Guid etapaId, EtapaTipo tipo, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(faltantes);
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

    private static (EnviarEtapaARevisionHandler Handler, TesisGrado Tesis, Guardado Guardado) Preparar(params string[] faltantes)
    {
        var estudiante = Guid.NewGuid();
        var tesis = TesisGrado.Crear(estudiante, "Titulo", "Resumen", Ahora);
        var guardado = new Guardado();
        var handler = new EnviarEtapaARevisionHandler(
            new Repositorio(tesis),
            new DecisionesVacias(),
            new Administradores(),
            new SinAsignaciones(),
            new NotificadorEnMemoria(),
            new Requisitos(faltantes),
            new Actual(estudiante, nameof(Rol.Estudiante)),
            new Reloj(),
            guardado,
            new AuditoriaEnMemoria(),
            NullLogger<EnviarEtapaARevisionHandler>.Instance);

        return (handler, tesis, guardado);
    }

    [Fact]
    [Trait("AC", "6.2")]
    public async Task Con_documentos_faltantes_lanza_EtapaIncompleta_con_la_lista_y_no_cambia_el_estado()
    {
        var (handler, tesis, guardado) = Preparar("Propuesta firmada", "Carta de aval");

        var ex = await Assert.ThrowsAsync<EtapaIncompletaException>(
            () => handler.HandleAsync(new EnviarEtapaARevisionCommand(tesis.Id, 1), CancellationToken.None));

        Assert.Equal(["Propuesta firmada", "Carta de aval"], ex.Faltantes);
        Assert.Equal(EstadoEtapa.Pendiente, tesis.EtapaActual.Estado);
        Assert.Equal(0, guardado.Guardados);
    }

    [Fact]
    [Trait("AC", "6.1")]
    public async Task Sin_faltantes_envia_a_revision_y_guarda()
    {
        var (handler, tesis, guardado) = Preparar();

        var resultado = await handler.HandleAsync(new EnviarEtapaARevisionCommand(tesis.Id, 1), CancellationToken.None);

        Assert.Equal("EnRevision", resultado.EtapaActual.Estado);
        Assert.Equal(1, guardado.Guardados);
    }
}
