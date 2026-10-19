using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Notificaciones;

namespace SGT.Tesis.UnitTests.Domain;

public class NotificacionTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private static NotificacionPendiente Nueva() => NotificacionPendiente.Crear("ana@institucion.edu", "Asunto", "Cuerpo", Ahora);

    [Fact]
    [Trait("AC", "15.1")]
    public void Una_notificacion_nueva_esta_pendiente_y_lista_para_enviarse()
    {
        var n = Nueva();

        Assert.Equal(EstadoNotificacion.Pendiente, n.Estado);
        Assert.True(n.DebeEnviarse(Ahora));
        Assert.Equal(0, n.Intentos);
    }

    [Fact]
    [Trait("AC", "15.2")]
    public void Cada_fallo_duplica_la_espera_y_tras_agotar_los_intentos_queda_fallida()
    {
        var n = Nueva();
        var base60 = TimeSpan.FromSeconds(60);

        n.RegistrarFallo("error 1", Ahora, 3, base60);
        Assert.Equal(Ahora.AddSeconds(60), n.ProximoIntentoEn);
        Assert.False(n.DebeEnviarse(Ahora));
        Assert.True(n.DebeEnviarse(Ahora.AddSeconds(60)));

        n.RegistrarFallo("error 2", Ahora, 3, base60);
        Assert.Equal(Ahora.AddSeconds(120), n.ProximoIntentoEn);

        n.RegistrarFallo("error 3", Ahora, 3, base60);
        Assert.Equal(EstadoNotificacion.Fallida, n.Estado);
        Assert.False(n.DebeEnviarse(Ahora.AddDays(1)));
        Assert.Equal(3, n.Intentos);
    }

    [Fact]
    [Trait("AC", "15.2")]
    public void Marcar_como_enviada_limpia_el_error()
    {
        var n = Nueva();
        n.RegistrarFallo("falló", Ahora, 5, TimeSpan.FromSeconds(1));

        n.MarcarEnviada(Ahora.AddMinutes(1));

        Assert.Equal(EstadoNotificacion.Enviada, n.Estado);
        Assert.Null(n.UltimoError);
        Assert.Equal(Ahora.AddMinutes(1), n.EnviadaEn);
        Assert.False(n.DebeEnviarse(Ahora.AddDays(1)));
    }

    [Fact]
    public void El_error_largo_se_recorta()
    {
        var n = Nueva();

        n.RegistrarFallo(new string('e', 5000), Ahora, 5, TimeSpan.FromSeconds(1));

        Assert.Equal(NotificacionPendiente.ErrorMaxLength, n.UltimoError!.Length);
    }

    [Theory]
    [InlineData("sin-arroba", "Asunto", "Cuerpo")]
    [InlineData("", "Asunto", "Cuerpo")]
    [InlineData("a@b.edu", "", "Cuerpo")]
    [InlineData("a@b.edu", "Asunto", " ")]
    public void Datos_invalidos_lanzan_excepcion(string destinatario, string asunto, string cuerpo)
    {
        Assert.Throws<DomainException>(() => NotificacionPendiente.Crear(destinatario, asunto, cuerpo, Ahora));
    }

    [Fact]
    [Trait("AC", "15.3")]
    public void Las_plantillas_solo_mencionan_la_etapa_y_el_tipo_de_evento()
    {
        foreach (var tipo in Enum.GetValues<EtapaTipo>())
        {
            var textos = new[]
            {
                Plantillas.EtapaEnRevision(tipo),
                Plantillas.EvaluadorAsignado(tipo),
                Plantillas.Decision(tipo, ResultadoDecision.Aprobada),
                Plantillas.Decision(tipo, ResultadoDecision.Observada),
                Plantillas.Decision(tipo, ResultadoDecision.Rechazada),
            };

            Assert.All(textos, t =>
            {
                Assert.Contains(Plantillas.NombreEtapa(tipo), t.Asunto + t.Cuerpo);
                Assert.True(t.Asunto.Length <= NotificacionPendiente.AsuntoMaxLength);
                Assert.True(t.Cuerpo.Length <= NotificacionPendiente.CuerpoMaxLength);
            });
        }
    }
}
