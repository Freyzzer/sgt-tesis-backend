using System.Reflection;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.Evaluacion;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

public class DecisionTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    private static TesisGrado EnRevision()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);

        return tesis;
    }

    [Fact]
    [Trait("AC", "11.4")]
    public void Aprobar_cierra_la_etapa_y_abre_la_siguiente_en_Pendiente()
    {
        var tesis = EnRevision();

        var decision = tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora.AddDays(1));

        Assert.Equal(EstadoEtapa.Aprobado, tesis.ObtenerEtapa(EtapaTipo.Estudiante).Estado);
        Assert.Equal(Ahora.AddDays(1), tesis.ObtenerEtapa(EtapaTipo.Estudiante).FechaCierre);
        Assert.Equal(EtapaTipo.Propuesta, tesis.EtapaActual.Tipo);
        Assert.Equal(EstadoEtapa.Pendiente, tesis.EtapaActual.Estado);
        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        Assert.Equal(ResultadoDecision.Aprobada, decision.Resultado);
        Assert.Equal(Admin, decision.AdministradorId);
        Assert.Equal(tesis.ObtenerEtapa(EtapaTipo.Estudiante).Id, decision.EtapaId);
    }

    [Theory]
    [Trait("AC", "11.3")]
    [InlineData(ResultadoDecision.Observada)]
    [InlineData(ResultadoDecision.Rechazada)]
    public void Observar_o_rechazar_exige_justificacion(ResultadoDecision resultado)
    {
        var tesis = EnRevision();

        Assert.Throws<DomainException>(() => tesis.Decidir(EtapaTipo.Estudiante, resultado, Admin, "   ", Ahora));
        Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }

    [Fact]
    [Trait("AC", "11.3")]
    public void La_justificacion_no_puede_superar_el_maximo()
    {
        Assert.Throws<DomainException>(() => Decision.Crear(Guid.NewGuid(), Admin, ResultadoDecision.Observada, new string('a', Decision.JustificacionMaxLength + 1), Ahora));
        Decision.Crear(Guid.NewGuid(), Admin, ResultadoDecision.Observada, new string('a', Decision.JustificacionMaxLength), Ahora);
    }

    [Fact]
    public void Observar_devuelve_la_etapa_al_estudiante_que_puede_reenviarla()
    {
        var tesis = EnRevision();

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Observada, Admin, "Falta el aval.", Ahora);

        Assert.Equal(EstadoEtapa.Observado, tesis.EtapaActual.Estado);
        Assert.Null(tesis.EtapaActual.FechaCierre);
        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        Assert.Single(tesis.Etapas);

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);

        Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }

    [Fact]
    public void Rechazar_cierra_la_etapa_y_rechaza_la_tesis()
    {
        var tesis = EnRevision();

        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Rechazada, Admin, "No cumple requisitos.", Ahora);

        Assert.Equal(EstadoEtapa.Rechazado, tesis.EtapaActual.Estado);
        Assert.Equal(EstadoTesis.Rechazada, tesis.Estado);
        Assert.NotNull(tesis.EtapaActual.FechaCierre);
        Assert.Throws<TransicionInvalidaException>(() => tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora));
    }

    [Fact]
    [Trait("AC", "11.2")]
    public void Solo_se_decide_sobre_una_etapa_en_revision()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);

        Assert.Throws<TransicionInvalidaException>(() => tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora));

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora);

        Assert.Throws<TransicionInvalidaException>(() => tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora));
    }

    [Fact]
    [Trait("AC", "11.4")]
    public void No_se_decide_sobre_una_etapa_que_no_es_la_actual()
    {
        var tesis = EnRevision();

        Assert.Throws<TransicionInvalidaException>(() => tesis.Decidir(EtapaTipo.Consejo, ResultadoDecision.Aprobada, Admin, null, Ahora));
    }

    [Fact]
    [Trait("AC", "11.4")]
    public void Aprobar_las_siete_etapas_finaliza_la_tesis()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);

        foreach (var tipo in Enum.GetValues<EtapaTipo>())
        {
            tesis.EnviarEtapaARevision(tipo, Ahora);
            tesis.Decidir(tipo, ResultadoDecision.Aprobada, Admin, null, Ahora);
        }

        Assert.Equal(EstadoTesis.Finalizada, tesis.Estado);
        Assert.Equal(7, tesis.Etapas.Count);
        Assert.All(tesis.Etapas, e => Assert.Equal(EstadoEtapa.Aprobado, e.Estado));
        Assert.Throws<TransicionInvalidaException>(() => tesis.Decidir(EtapaTipo.RequisitosFinales, ResultadoDecision.Aprobada, Admin, null, Ahora));
    }

    [Fact]
    [Trait("AC", "11.7")]
    public void Cada_cambio_de_estado_incrementa_la_revision_usada_como_token_de_concurrencia()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        var inicial = tesis.EtapaActual.Revision;

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante, Ahora);
        var enviada = tesis.EtapaActual.Revision;
        tesis.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Aprobada, Admin, null, Ahora);

        Assert.True(enviada > inicial);
        Assert.True(tesis.ObtenerEtapa(EtapaTipo.Estudiante).Revision > enviada);
    }

    [Fact]
    [Trait("AC", "11.6")]
    public void Decision_no_expone_mutadores_publicos()
    {
        var mutadores = typeof(Decision).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name);

        Assert.Empty(mutadores);
    }

    [Fact]
    public void Un_resultado_inexistente_lanza_excepcion()
    {
        Assert.Throws<DomainException>(() => Decision.Crear(Guid.NewGuid(), Admin, (ResultadoDecision)9, "x", Ahora));
    }

    [Fact]
    [Trait("AC", "9.1")]
    public void Retirar_una_asignacion_la_desactiva_una_sola_vez()
    {
        var asignacion = AsignacionEvaluador.Crear(Guid.NewGuid(), Guid.NewGuid(), Admin, Ahora);

        asignacion.Retirar(Ahora.AddDays(1));
        asignacion.Retirar(Ahora.AddDays(2));

        Assert.False(asignacion.Activa);
        Assert.Equal(Ahora.AddDays(1), asignacion.FechaRetiro);
    }

    [Fact]
    [Trait("AC", "10.2")]
    public void El_veredicto_valida_concepto_comentarios_y_version()
    {
        var etapa = Guid.NewGuid();
        var evaluador = Guid.NewGuid();

        Veredicto.Crear(etapa, evaluador, ConceptoVeredicto.Favorable, new string('a', Veredicto.ComentariosMaxLength), 1);

        Assert.Throws<DomainException>(() => Veredicto.Crear(etapa, evaluador, ConceptoVeredicto.Favorable, new string('a', Veredicto.ComentariosMaxLength + 1), 1));
        Assert.Throws<DomainException>(() => Veredicto.Crear(etapa, evaluador, ConceptoVeredicto.Favorable, " ", 1));
        Assert.Throws<DomainException>(() => Veredicto.Crear(etapa, evaluador, (ConceptoVeredicto)7, "ok", 1));
        Assert.Throws<DomainException>(() => Veredicto.Crear(etapa, evaluador, ConceptoVeredicto.Favorable, "ok", 0));
    }
}
