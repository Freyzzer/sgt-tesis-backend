using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.UnitTests.Domain;

public class BaseEntityTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("AC", "17.1")]
    public void MarkCreated_registra_fecha_y_autor()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        var ahora = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        var autor = Guid.NewGuid();

        tesis.MarkCreated(ahora, autor);

        Assert.Equal(ahora, tesis.CreatedAt);
        Assert.Equal(autor, tesis.CreatedBy);
        Assert.Null(tesis.UpdatedAt);
        Assert.Null(tesis.UpdatedBy);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public void MarkUpdated_registra_fecha_y_autor_sin_alterar_la_creacion()
    {
        var tesis = TesisGrado.Crear(Guid.NewGuid(), "Titulo", "Resumen", Ahora);
        var creada = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        var autor = Guid.NewGuid();
        var editor = Guid.NewGuid();
        tesis.MarkCreated(creada, autor);

        var editada = creada.AddHours(1);
        tesis.MarkUpdated(editada, editor);

        Assert.Equal(creada, tesis.CreatedAt);
        Assert.Equal(autor, tesis.CreatedBy);
        Assert.Equal(editada, tesis.UpdatedAt);
        Assert.Equal(editor, tesis.UpdatedBy);
    }
}
