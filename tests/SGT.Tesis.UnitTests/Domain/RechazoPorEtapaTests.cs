using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

/// <summary>
/// ADR-005: un rechazo se puede reintentar salvo en las etapas que la política declara definitivas
/// (por defecto, Pasantía y Artículo).
/// </summary>
public class RechazoPorEtapaTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    /// <summary>Lleva la tesis hasta la etapa indicada (aprobando las anteriores) y la deja en revisión.</summary>
    private static TesisGrado EnRevisionEn(EtapaTipo destino)
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);

        for (var tipo = EtapaTipo.Estudiante; tipo < destino; tipo++)
        {
            tesis.EnviarEtapaARevision(tipo, Ahora);
            tesis.Decidir(tipo, ResultadoDecision.Aprobada, Admin, null, Ahora);
        }

        tesis.EnviarEtapaARevision(destino, Ahora);

        return tesis;
    }

    [Theory]
    [InlineData(EtapaTipo.Estudiante)]
    [InlineData(EtapaTipo.Propuesta)]
    [InlineData(EtapaTipo.Consejo)]
    [InlineData(EtapaTipo.Evaluacion)]
    [InlineData(EtapaTipo.RequisitosFinales)]
    public void Un_rechazo_reintentable_deja_la_tesis_en_curso_y_la_etapa_abierta_para_corregir(EtapaTipo tipo)
    {
        var tesis = EnRevisionEn(tipo);

        tesis.Decidir(tipo, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora);

        var etapa = tesis.ObtenerEtapa(tipo);
        Assert.Equal(EstadoEtapa.Rechazado, etapa.Estado);
        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        Assert.Null(etapa.FechaCierre);
        Assert.Equal(tipo, tesis.EtapaActual.Tipo);
        Assert.True(etapa.AdmiteDocumentos);
        Assert.Same(etapa, tesis.ExigirEtapaEditable(tipo));
    }

    [Fact]
    public void Tras_un_rechazo_reintentable_el_estudiante_reenvia_la_misma_etapa_cuantas_veces_haga_falta()
    {
        var tesis = EnRevisionEn(EtapaTipo.Propuesta);

        for (var intento = 0; intento < 4; intento++)
        {
            tesis.Decidir(EtapaTipo.Propuesta, ResultadoDecision.Rechazada, Admin, $"Intento {intento}.", Ahora);
            tesis.EnviarEtapaARevision(EtapaTipo.Propuesta, Ahora);

            Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
            Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        }

        tesis.Decidir(EtapaTipo.Propuesta, ResultadoDecision.Aprobada, Admin, null, Ahora);

        Assert.Equal(EtapaTipo.Consejo, tesis.EtapaActual.Tipo);
        Assert.NotNull(tesis.ObtenerEtapa(EtapaTipo.Propuesta).FechaCierre);
    }

    [Theory]
    [InlineData(EtapaTipo.Pasantia)]
    [InlineData(EtapaTipo.Articulo)]
    public void Un_rechazo_definitivo_cierra_la_etapa_y_rechaza_la_tesis(EtapaTipo tipo)
    {
        var tesis = EnRevisionEn(tipo);

        tesis.Decidir(tipo, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora);

        var etapa = tesis.ObtenerEtapa(tipo);
        Assert.Equal(EstadoEtapa.Rechazado, etapa.Estado);
        Assert.Equal(EstadoTesis.Rechazada, tesis.Estado);
        Assert.NotNull(etapa.FechaCierre);
        Assert.Throws<TransicionInvalidaException>(() => tesis.EnviarEtapaARevision(tipo, Ahora));
        Assert.Throws<TransicionInvalidaException>(() => tesis.ExigirEtapaEditable(tipo));
    }

    [Fact]
    public void La_politica_manda_sobre_los_valores_por_defecto()
    {
        var definitiva = new PoliticaEtapa(PermiteReintento: false, MaxEvaluadores: 3, ExigeUnanimidad: true);
        var reintentable = new PoliticaEtapa(PermiteReintento: true, MaxEvaluadores: 3, ExigeUnanimidad: true);

        var propuestaDefinitiva = EnRevisionEn(EtapaTipo.Propuesta);
        propuestaDefinitiva.Decidir(EtapaTipo.Propuesta, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora, definitiva);

        var pasantiaReintentable = EnRevisionEn(EtapaTipo.Pasantia);
        pasantiaReintentable.Decidir(EtapaTipo.Pasantia, ResultadoDecision.Rechazada, Admin, "No cumple.", Ahora, reintentable);

        Assert.Equal(EstadoTesis.Rechazada, propuestaDefinitiva.Estado);
        Assert.Equal(EstadoTesis.EnCurso, pasantiaReintentable.Estado);
    }

    [Fact]
    public void Observar_y_aprobar_no_dependen_de_la_politica_de_reintento()
    {
        var definitiva = new PoliticaEtapa(PermiteReintento: false, MaxEvaluadores: 3, ExigeUnanimidad: true);
        var tesis = EnRevisionEn(EtapaTipo.Pasantia);

        tesis.Decidir(EtapaTipo.Pasantia, ResultadoDecision.Observada, Admin, "Corregir.", Ahora, definitiva);

        Assert.Equal(EstadoEtapa.Observado, tesis.EtapaActual.Estado);
        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
    }

    [Fact]
    public void Los_valores_por_defecto_son_los_acordados_con_el_Product_Owner()
    {
        foreach (var tipo in Enum.GetValues<EtapaTipo>())
        {
            var p = PoliticaEtapa.PorDefecto(tipo);

            Assert.Equal(tipo is not (EtapaTipo.Pasantia or EtapaTipo.Articulo), p.PermiteReintento);
            Assert.Equal(3, p.MaxEvaluadores);
            Assert.True(p.ExigeUnanimidad);
        }
    }
}
