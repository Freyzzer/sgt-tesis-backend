using SGT.Tesis.Application.Evaluacion;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

/// <summary>ADR-005: cada envío abre una ronda y aprobar exige unanimidad Favorable en la ronda actual.</summary>
public class RondasYUnanimidadTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    private static TesisGrado Nueva() => TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);

    // ---------- Rondas ----------

    [Fact]
    public void La_ronda_es_cero_antes_de_enviar_y_cada_envio_abre_una_nueva()
    {
        var tesis = Nueva();
        Assert.Equal(0, tesis.EtapaActual.Ronda);

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        Assert.Equal(1, tesis.EtapaActual.Ronda);

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Observada, Admin, "Corregir.", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        Assert.Equal(2, tesis.EtapaActual.Ronda);

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        Assert.Equal(3, tesis.EtapaActual.Ronda);
    }

    [Fact]
    public void Cada_decision_queda_registrada_con_la_ronda_en_que_se_tomo()
    {
        var tesis = Nueva();
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        var primera = tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Observada, Admin, "Corregir.", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        var segunda = tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora);

        Assert.Equal(1, primera.Ronda);
        Assert.Equal(2, segunda.Ronda);
    }

    [Fact]
    public void Un_veredicto_guarda_su_ronda_y_no_admite_una_invalida()
    {
        var v = Veredicto.Crear(Guid.NewGuid(), Guid.NewGuid(), ConceptoVeredicto.Favorable, "Bien.", 1, 3);

        Assert.Equal(3, v.Ronda);
        Assert.Throws<DomainException>(() => Veredicto.Crear(Guid.NewGuid(), Guid.NewGuid(), ConceptoVeredicto.Favorable, "Bien.", 1, 0));
    }

    // ---------- Unanimidad ----------

    private static TesisGrado EnRevision()
    {
        var tesis = Nueva();
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);

        return tesis;
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 2)]
    [InlineData(2, 1)]
    public void Aprobar_sin_que_todos_los_evaluadores_sean_favorables_se_rechaza_con_un_mensaje_claro(int activos, int favorables)
    {
        var tesis = EnRevision();

        var ex = Assert.Throws<DomainException>(() =>
            tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora, evaluacion: new ResumenEvaluacion(activos, favorables)));

        Assert.Contains($"{favorables} de {activos}", ex.Message, StringComparison.Ordinal);
        Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    public void Aprobar_con_todos_los_evaluadores_favorables_funciona(int activos, int favorables)
    {
        var tesis = EnRevision();

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora, evaluacion: new ResumenEvaluacion(activos, favorables));

        Assert.Equal(EstadoEtapa.Aprobado, tesis.ObtenerEtapa(EtapaTipo.Estudiante).Estado);
    }

    [Fact]
    public void Sin_evaluadores_asignados_el_administrador_aprueba_directamente()
    {
        var tesis = EnRevision();

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora, evaluacion: new ResumenEvaluacion(0, 0));

        Assert.Equal(EstadoEtapa.Aprobado, tesis.ObtenerEtapa(EtapaTipo.Estudiante).Estado);
    }

    [Fact]
    public void Si_la_politica_no_exige_unanimidad_no_se_aplica_la_regla()
    {
        var tesis = EnRevision();
        var sinUnanimidad = new PoliticaEtapa(PermiteReintento: true, MaxEvaluadores: 3, ExigeUnanimidad: false);

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora, sinUnanimidad, new ResumenEvaluacion(3, 0));

        Assert.Equal(EstadoEtapa.Aprobado, tesis.ObtenerEtapa(EtapaTipo.Estudiante).Estado);
    }

    [Theory]
    [InlineData(ResultadoDecision.Observada)]
    [InlineData(ResultadoDecision.Rechazada)]
    public void Observar_o_rechazar_no_exigen_unanimidad(ResultadoDecision resultado)
    {
        var tesis = EnRevision();

        tesis.Decidir(EtapaTipo.Estudiante, resultado, Admin, "Motivo.", Ahora, evaluacion: new ResumenEvaluacion(3, 0));

        Assert.NotEqual(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }

    [Fact]
    public void El_error_de_estado_prevalece_sobre_el_de_unanimidad()
    {
        var tesis = Nueva(); // etapa Pendiente: aún no se envió

        Assert.Throws<TransicionInvalidaException>(() =>
            tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora, evaluacion: new ResumenEvaluacion(2, 0)));
    }

    // ---------- Veredictos vigentes por ronda ----------

    private static Veredicto UnVeredicto(Guid evaluador, ConceptoVeredicto concepto, int version, int ronda) =>
        Veredicto.Crear(Guid.NewGuid(), evaluador, concepto, "Comentario.", version, ronda);

    [Fact]
    public void Vigente_es_el_de_mayor_version_de_la_ronda_y_los_de_otras_rondas_no_cuentan()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var lista = new[]
        {
            UnVeredicto(a, ConceptoVeredicto.Favorable, 1, 1),
            UnVeredicto(a, ConceptoVeredicto.ConObservaciones, 2, 2),
            UnVeredicto(a, ConceptoVeredicto.Favorable, 3, 2),
            UnVeredicto(b, ConceptoVeredicto.Favorable, 1, 1),
        };

        var vigentes = CalculoDeEvaluacion.VigentesDeLaRonda(lista, 2);

        Assert.Single(vigentes);
        Assert.Equal(3, vigentes[a].Version);
        Assert.False(vigentes.ContainsKey(b));
    }

    [Fact]
    public void El_resumen_cuenta_solo_evaluadores_activos_con_veredicto_favorable_en_la_ronda_actual()
    {
        var tesis = EnRevision();
        var etapa = tesis.EtapaActual;
        var admin = Guid.NewGuid();
        var (a, b, c, retirado) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var asignaciones = new[] { a, b, c, retirado }.Select(id => AsignacionEvaluador.Crear(etapa.Id, id, admin, Ahora)).ToList();
        asignaciones[3].Retirar(Ahora);

        var veredictos = new[]
        {
            UnVeredicto(a, ConceptoVeredicto.Favorable, 1, etapa.Ronda),
            UnVeredicto(b, ConceptoVeredicto.ConObservaciones, 1, etapa.Ronda),
            UnVeredicto(retirado, ConceptoVeredicto.Favorable, 1, etapa.Ronda), // ya no es evaluador
        };

        var resumen = CalculoDeEvaluacion.Resumir(etapa, asignaciones, veredictos);

        Assert.Equal(3, resumen.Activos);
        Assert.Equal(1, resumen.Favorables);
        Assert.Equal(2, resumen.Pendientes);
        Assert.False(resumen.EsUnanime);
    }
}
