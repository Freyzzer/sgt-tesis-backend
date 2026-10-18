using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

public class EtapasTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private static TesisGrado Nueva() => TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);

    [Fact]
    [Trait("AC", "4.1")]
    public void Una_tesis_nueva_tiene_solo_la_etapa_Estudiante_pendiente_y_en_curso()
    {
        var tesis = Nueva();

        Assert.Equal(EstadoTesis.EnCurso, tesis.Estado);
        Assert.Single(tesis.Etapas);
        Assert.Equal(EtapaTipo.Estudiante, tesis.EtapaActual.Tipo);
        Assert.Equal(EstadoEtapa.Pendiente, tesis.EtapaActual.Estado);
        Assert.Equal(Ahora, tesis.EtapaActual.FechaApertura);
        Assert.Equal(tesis.Id, tesis.EtapaActual.TesisId);
    }

    [Fact]
    [Trait("AC", "6.1")]
    public void Enviar_la_etapa_actual_pendiente_la_pasa_a_EnRevision()
    {
        var tesis = Nueva();

        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante);

        Assert.Equal(EstadoEtapa.EnRevision, tesis.EtapaActual.Estado);
    }

    [Fact]
    [Trait("AC", "6.3")]
    public void Enviar_una_etapa_que_no_es_la_actual_es_una_transicion_invalida()
    {
        var tesis = Nueva();

        Assert.Throws<TransicionInvalidaException>(() => tesis.EnviarEtapaARevision(EtapaTipo.Propuesta));
        Assert.Equal(EstadoEtapa.Pendiente, tesis.EtapaActual.Estado);
    }

    [Fact]
    [Trait("AC", "6.3")]
    public void Enviar_dos_veces_la_misma_etapa_es_una_transicion_invalida()
    {
        var tesis = Nueva();
        tesis.EnviarEtapaARevision(EtapaTipo.Estudiante);

        var ex = Assert.Throws<TransicionInvalidaException>(() => tesis.EnviarEtapaARevision(EtapaTipo.Estudiante));

        Assert.Contains("revisión", ex.Message);
    }

    [Fact]
    public void Un_tipo_de_etapa_inexistente_no_se_puede_abrir()
    {
        Assert.Throws<TransicionInvalidaException>(() => Nueva().ObtenerEtapa((EtapaTipo)99));
    }

    [Fact]
    public void Las_etapas_se_enumeran_del_1_al_7()
    {
        Assert.Equal(Enumerable.Range(1, 7), Enum.GetValues<EtapaTipo>().Select(e => (int)e));
    }
}
