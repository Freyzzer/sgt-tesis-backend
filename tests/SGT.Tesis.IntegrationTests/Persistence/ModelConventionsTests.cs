using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.IntegrationTests.Persistence;

public class ModelConventionsTests
{
    // Construir el modelo no abre conexion: la cadena es solo un marcador.
    private static readonly Microsoft.EntityFrameworkCore.Metadata.IModel Model = CrearModelo();

    private static Microsoft.EntityFrameworkCore.Metadata.IModel CrearModelo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(none);Database=modelo;Trusted_Connection=True;")
            .Options;

        using var context = new AppDbContext(options);
        return context.Model;
    }

    [Fact]
    [Trait("AC", "17.1")]
    public void Toda_entidad_hereda_de_BaseEntity()
    {
        var infractoras = Model.GetEntityTypes()
            .Where(e => !typeof(BaseEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.Empty(infractoras);
    }

    [Fact]
    [Trait("AC", "17.1")]
    public void Toda_clave_primaria_es_guid_generado_por_la_aplicacion()
    {
        foreach (var entity in Model.GetEntityTypes())
        {
            var id = entity.FindProperty(nameof(BaseEntity.Id));

            Assert.NotNull(id);
            Assert.Equal(typeof(Guid), id.ClrType);
            Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, id.ValueGenerated);
        }
    }

    [Fact]
    [Trait("AC", "17.2")]
    public void Todo_campo_de_texto_tiene_MaxLength_explicito()
    {
        var sinLimite = Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null)
            .Select(p => $"{p.DeclaringType.DisplayName()}.{p.Name}")
            .ToList();

        Assert.Empty(sinLimite);
    }
}
