using Microsoft.Extensions.Configuration;
using SGT.Tesis.Infrastructure.Documentos;

namespace SGT.Tesis.IntegrationTests.Documentos;

/// <summary>
/// Decisión del Product Owner (2026-10-19): por ahora solo son obligatorios el documento de la propuesta y el de la
/// pasantía. Se verifica contra el <c>appsettings.json</c> real de la API, no contra una configuración de prueba.
/// </summary>
public class RequisitosAcordadosTests
{
    private static string AppSettingsDeLaApi()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SGT.Tesis.slnx")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("No se encontró SGT.Tesis.slnx."), "src", "SGT.Tesis.API", "appsettings.json");
    }

    [Fact]
    public void Por_defecto_solo_la_propuesta_y_la_pasantia_son_obligatorias()
    {
        var opciones = new ConfigurationBuilder().AddJsonFile(AppSettingsDeLaApi()).Build()
            .GetSection(DocumentosOptions.SectionName).Get<DocumentosOptions>()!;

        Assert.Equal(["Pasantia", "Propuesta"], opciones.Requisitos.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["Propuesta"], opciones.Requisitos["Propuesta"]);
        Assert.Equal(["Pasantía"], opciones.Requisitos["Pasantia"]);
    }
}
