using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("AC", "2.1")]
    public void Un_token_nuevo_esta_activo_hasta_su_expiracion()
    {
        var token = RefreshToken.Crear(Guid.NewGuid(), "hash", Ahora.AddDays(7));

        Assert.True(token.EstaActivo(Ahora));
        Assert.False(token.EstaActivo(Ahora.AddDays(8)));
    }

    [Fact]
    [Trait("AC", "2.2")]
    public void Revocar_lo_inactiva_y_conserva_la_primera_fecha_de_revocacion()
    {
        var token = RefreshToken.Crear(Guid.NewGuid(), "hash", Ahora.AddDays(7));

        token.Revocar(Ahora);
        token.Revocar(Ahora.AddHours(1));

        Assert.False(token.EstaActivo(Ahora));
        Assert.Equal(Ahora, token.RevocadoEn);
    }

    [Fact]
    public void Un_token_rotado_hereda_la_familia()
    {
        var primero = RefreshToken.Crear(Guid.NewGuid(), "h1", Ahora.AddDays(7));
        var segundo = RefreshToken.Crear(primero.UsuarioId, "h2", Ahora.AddDays(7), primero.FamiliaId);

        Assert.Equal(primero.FamiliaId, segundo.FamiliaId);
    }

    [Fact]
    public void Sin_usuario_o_sin_hash_lanza_excepcion()
    {
        Assert.Throws<DomainException>(() => RefreshToken.Crear(Guid.Empty, "hash", Ahora));
        Assert.Throws<DomainException>(() => RefreshToken.Crear(Guid.NewGuid(), "", Ahora));
    }
}
