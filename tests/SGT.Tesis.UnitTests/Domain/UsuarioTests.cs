using SGT.Tesis.Domain.Common;
using SGT.Tesis.Domain.Identidad;

namespace SGT.Tesis.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private static Usuario Nuevo() => Usuario.Crear("Ana", "Pérez", "Ana.Perez@Institucion.edu", "hash", Rol.Estudiante);

    [Fact]
    [Trait("AC", "3.2")]
    public void El_correo_se_normaliza_a_minusculas()
    {
        Assert.Equal("ana.perez@institucion.edu", Nuevo().Correo);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("@institucion.edu")]
    [InlineData("ana@")]
    public void Correo_con_formato_invalido_lanza_excepcion(string correo)
    {
        Assert.Throws<DomainException>(() => Usuario.Crear("Ana", "Pérez", correo, "hash", Rol.Estudiante));
    }

    [Fact]
    public void Rol_inexistente_lanza_excepcion()
    {
        Assert.Throws<DomainException>(() => Usuario.Crear("Ana", "Pérez", "a@b.edu", "hash", (Rol)99));
    }

    [Fact]
    public void Campos_obligatorios_y_longitudes_se_validan()
    {
        Assert.Throws<DomainException>(() => Usuario.Crear("", "Pérez", "a@b.edu", "hash", Rol.Estudiante));
        Assert.Throws<DomainException>(() => Usuario.Crear("Ana", new string('a', Usuario.ApellidosMaxLength + 1), "a@b.edu", "hash", Rol.Estudiante));
        Assert.Throws<DomainException>(() => Usuario.Crear("Ana", "Pérez", "a@b.edu", "", Rol.Estudiante));
    }

    [Fact]
    [Trait("AC", "1.3")]
    public void Al_quinto_fallo_se_bloquea_por_15_minutos()
    {
        var usuario = Nuevo();

        for (var i = 1; i < Usuario.MaxIntentosFallidos; i++)
        {
            Assert.False(usuario.RegistrarFalloDeAcceso(Ahora));
        }

        Assert.True(usuario.RegistrarFalloDeAcceso(Ahora));
        Assert.True(usuario.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Ahora.AddMinutes(16)));
    }

    [Fact]
    [Trait("AC", "1.3")]
    public void El_acceso_exitoso_reinicia_contador_y_bloqueo()
    {
        var usuario = Nuevo();

        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
        {
            usuario.RegistrarFalloDeAcceso(Ahora);
        }

        usuario.RegistrarAccesoExitoso();

        Assert.False(usuario.EstaBloqueado(Ahora));
        Assert.Equal(0, usuario.IntentosFallidos);
    }

    [Fact]
    [Trait("AC", "3.3")]
    public void Desactivar_y_activar_cambian_el_estado()
    {
        var usuario = Nuevo();

        usuario.Desactivar();
        Assert.False(usuario.Activo);

        usuario.Activar();
        Assert.True(usuario.Activo);
    }

    [Fact]
    public void Cambiar_a_rol_inexistente_lanza_excepcion()
    {
        Assert.Throws<DomainException>(() => Nuevo().CambiarRol((Rol)0));
    }
}
