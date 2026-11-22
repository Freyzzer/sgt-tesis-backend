using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

/// <summary>ADR-005: el administrador puede reabrir una etapa rechazada, también una definitiva.</summary>
public class ReaperturaTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly PoliticaEtapa Definitiva = new(PermiteReintento: false, MaxEvaluadores: 3, ExigeUnanimidad: true);

    private static TesisGrado Rechazada(PoliticaEtapa? politica = null)
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora, politica);

        return tesis;
    }

    [Fact]
    public void Reabrir_un_rechazo_definitivo_devuelve_la_tesis_al_curso_y_la_etapa_al_estudiante()
    {
        var tesis = Rechazada(Definitiva);
        Assert.Equal(EstadoTesis.Rechazada, tesis.Estado);
        Assert.NotNull(tesis.EtapaActual.FechaCierre);

        var decision = tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Error al registrar el rechazo.", Ahora.AddDays(1));

        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        Assert.Equal(EstadoEtapa.Observado, tesis.EtapaActual.Estado);
        Assert.Null(tesis.EtapaActual.FechaCierre);
        Assert.Equal(ResultadoDecision.Reabierta, decision.Resultado);
        Assert.Equal("Error al registrar el rechazo.", decision.Justificacion);
        Assert.Equal(1, decision.Ronda);
        Assert.Equal(tesis.EtapaActual.Id, decision.EtapaId);
    }

    [Fact]
    public void Tras_reabrir_el_estudiante_reenvia_en_una_ronda_nueva_y_el_proceso_continua()
    {
        var tesis = Rechazada(Definitiva);
        tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Revisión de la decisión.", Ahora);

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        Assert.Equal(2, tesis.EtapaActual.Ronda);

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora);
        Assert.Equal(EtapaTipo.Propuesta, tesis.EtapaActual.Tipo);
    }

    [Fact]
    public void Tambien_se_puede_reabrir_un_rechazo_reintentable()
    {
        var tesis = Rechazada();

        tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Se acuerda otra oportunidad.", Ahora);

        Assert.Equal(EstadoEtapa.Observado, tesis.EtapaActual.Estado);
        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
    }

    [Theory]
    [InlineData(ResultadoDecision.Aprobada)]
    [InlineData(ResultadoDecision.Observada)]
    public void Solo_se_reabre_una_etapa_rechazada(ResultadoDecision previa)
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        tesis.Decidir(EtapaTipo.Estudiante, previa, Admin, "Motivo.", Ahora);

        Assert.Throws<TransicionInvalidaException>(() => tesis.Reabrir(tesis.EtapaActual.Tipo, Admin, "Motivo.", Ahora));
    }

    [Fact]
    public void No_se_reabre_una_etapa_pendiente_ni_en_revision()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        Assert.Throws<TransicionInvalidaException>(() => tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Motivo.", Ahora));

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        Assert.Throws<TransicionInvalidaException>(() => tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Motivo.", Ahora));
    }

    [Fact]
    public void La_justificacion_es_obligatoria_y_no_se_modifica_nada_si_falta()
    {
        var tesis = Rechazada(Definitiva);

        Assert.Throws<DomainException>(() => tesis.Reabrir(EtapaTipo.Estudiante, Admin, "   ", Ahora));

        Assert.Equal(EstadoTesis.Rechazada, tesis.Estado);
        Assert.Equal(EstadoEtapa.Rechazado, tesis.EtapaActual.Estado);
    }

    [Fact]
    public void Solo_se_reabre_la_etapa_actual()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora);

        Assert.Throws<TransicionInvalidaException>(() => tesis.Reabrir(EtapaTipo.Estudiante, Admin, "Motivo.", Ahora));
    }

    [Fact]
    public void Decidir_no_acepta_el_resultado_Reabierta()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);

        Assert.Throws<DomainException>(() => tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Reabierta, Admin, "Motivo.", Ahora));
        Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }
}
