using SGT.Tesis.Application.Notificaciones;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Domain.Notificaciones;

namespace SGT.Tesis.UnitTests.Domain;

public class ContrasenasDominioTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Una_cuenta_creada_con_contrasena_temporal_debe_cambiarla_y_al_hacerlo_se_levantan_los_bloqueos()
    {
        var usuario = Usuario.Crear("Ana", "Pérez", "ana@x.edu", "hash-temporal", Rol.Estudiante, debeCambiarPassword: true);
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            usuario.RegistrarFalloDeAcceso(Ahora);
        }

        Assert.True(usuario.DebeCambiarPassword);
        Assert.True(usuario.EstaBloqueado(Ahora));

        usuario.CambiarPassword("hash-nuevo");

        Assert.Equal("hash-nuevo", usuario.PasswordHash);
        Assert.False(usuario.DebeCambiarPassword);
        Assert.False(usuario.EstaBloqueado(Ahora));
        Assert.Equal(0, usuario.IntentosFallidos);
    }

    [Fact]
    public void Cambiar_a_un_hash_vacio_lanza_excepcion()
    {
        var usuario = Usuario.Crear("Ana", "Pérez", "ana@x.edu", "hash", Rol.Estudiante);

        Assert.Throws<DomainException>(() => usuario.CambiarPassword(" "));
        Assert.Equal("hash", usuario.PasswordHash);
    }

    [Fact]
    public void El_token_de_restablecimiento_vence_a_los_30_minutos_y_solo_se_usa_una_vez()
    {
        var token = PasswordResetToken.Crear(Guid.NewGuid(), "hash", Ahora);

        Assert.True(token.EstaVigente(Ahora.AddMinutes(29)));
        Assert.False(token.EstaVigente(Ahora.AddMinutes(30)));

        token.MarcarUsado(Ahora.AddMinutes(5));
        token.MarcarUsado(Ahora.AddMinutes(6));

        Assert.False(token.EstaVigente(Ahora.AddMinutes(10)));
        Assert.Equal(Ahora.AddMinutes(5), token.UsadoEn);
    }

    [Fact]
    public void El_token_exige_usuario_y_hash()
    {
        Assert.Throws<DomainException>(() => PasswordResetToken.Crear(Guid.Empty, "hash", Ahora));
        Assert.Throws<DomainException>(() => PasswordResetToken.Crear(Guid.NewGuid(), "", Ahora));
    }

    [Fact]
    public void La_notificacion_sensible_borra_su_cuerpo_al_enviarse_y_la_normal_no()
    {
        var sensible = NotificacionPendiente.Crear("a@b.edu", "Asunto", "secreto-123", Ahora, sensible: true);
        var normal = NotificacionPendiente.Crear("a@b.edu", "Asunto", "texto normal", Ahora);

        sensible.MarcarEnviada(Ahora);
        normal.MarcarEnviada(Ahora);

        Assert.Equal(NotificacionPendiente.CuerpoDepurado, sensible.Cuerpo);
        Assert.Equal("texto normal", normal.Cuerpo);
    }

    [Fact]
    public void La_notificacion_sensible_conserva_su_cuerpo_mientras_se_reintenta_y_lo_borra_al_fallar_definitivamente()
    {
        var n = NotificacionPendiente.Crear("a@b.edu", "Asunto", "secreto-123", Ahora, sensible: true);

        n.RegistrarFallo("error", Ahora, 2, TimeSpan.FromSeconds(10));
        Assert.Equal("secreto-123", n.Cuerpo);

        n.RegistrarFallo("error", Ahora, 2, TimeSpan.FromSeconds(10));
        Assert.Equal(EstadoNotificacion.Fallida, n.Estado);
        Assert.Equal(NotificacionPendiente.CuerpoDepurado, n.Cuerpo);
    }

    [Fact]
    public void La_plantilla_de_restablecimiento_usa_el_enlace_o_el_codigo_y_escapa_el_token()
    {
        var conEnlace = Plantillas.RestablecerPassword("https://app.test/r?token={token}", "a+b/c=");
        var sinEnlace = Plantillas.RestablecerPassword(null, "codigo-123");

        Assert.Contains("https://app.test/r?token=a%2Bb%2Fc%3D", conEnlace.Cuerpo);
        Assert.Contains("codigo-123", sinEnlace.Cuerpo);
        Assert.Contains("Use este código", sinEnlace.Cuerpo);
        Assert.All(new[] { conEnlace, sinEnlace }, t => Assert.Contains("30 minutos", t.Cuerpo));
    }
}
