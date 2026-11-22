using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Auditoria;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Reportes;

public class ExportacionEndpointsTests
{
    private const string Kpis = "/api/v1/dashboard/kpis/exportar";
    private const string Cola = "/api/v1/dashboard/cola-validacion/exportar";

    private static async Task<Guid> TesisEnRevisionAsync(ApiFactory factory, string titulo)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var cliente = await factory.ClienteComoAsync(correo);
        var id = (await (await cliente.PostAsJsonAsync("/api/v1/tesis", new { titulo, resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/v1/tesis/{id}/etapas/1/enviar", null)).StatusCode);
        return id;
    }

    private static async Task<(string Texto, HttpResponseMessage Respuesta)> DescargarAsync(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta, TestContext.Current.CancellationToken);
        var bytes = await respuesta.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        return (Encoding.UTF8.GetString(bytes), respuesta);
    }

    [Fact]
    public async Task Solo_el_administrador_exporta()
    {
        using var factory = new ApiFactory();
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await factory.ClienteComoAsync(correo);
        using var anonimo = factory.CreateClient();

        foreach (var ruta in new[] { Kpis, Cola })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        }
    }

    [Fact]
    public async Task La_cola_se_descarga_como_CSV_con_tildes_y_separador_a_eleccion()
    {
        using var factory = new ApiFactory();
        await TesisEnRevisionAsync(factory, "Análisis de datos, año 2026");
        using var admin = await factory.ClienteAdministradorAsync();

        var (coma, r1) = await DescargarAsync(admin, Cola);
        var (puntoYComa, _) = await DescargarAsync(admin, $"{Cola}?separador=PuntoYComa");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal("text/csv", r1.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", r1.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Matches(@"^cola-validacion-\d{8}-\d{4}\.csv$", r1.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty);
        Assert.StartsWith("﻿Tesis,Estudiante,Etapa,", coma, StringComparison.Ordinal);
        Assert.Contains("\"Análisis de datos, año 2026\"", coma, StringComparison.Ordinal);
        Assert.StartsWith("﻿Tesis;Estudiante;Etapa;", puntoYComa, StringComparison.Ordinal);
        Assert.Contains("Análisis de datos, año 2026;", puntoYComa, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Una_celda_que_empieza_como_formula_se_neutraliza()
    {
        using var factory = new ApiFactory();
        await TesisEnRevisionAsync(factory, "=HYPERLINK(\"http://x\")");
        using var admin = await factory.ClienteAdministradorAsync();

        var (csv, _) = await DescargarAsync(admin, Cola);

        Assert.DoesNotContain("\n=HYPERLINK", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(",=HYPERLINK", csv, StringComparison.Ordinal);
        Assert.Contains("'=HYPERLINK", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Los_indicadores_se_exportan_en_formato_largo_y_la_exportacion_queda_auditada()
    {
        using var factory = new ApiFactory();
        await TesisEnRevisionAsync(factory, "Titulo");
        using var admin = await factory.ClienteAdministradorAsync();

        var (csv, respuesta) = await DescargarAsync(admin, $"{Kpis}?diasEstancamiento=30&separador=PuntoYComa");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.StartsWith("﻿Sección;Concepto;Métrica;Valor", csv, StringComparison.Ordinal);
        Assert.Contains("Resumen;Todas;Tesis registradas;", csv, StringComparison.Ordinal);
        Assert.Contains("Resumen;Más de 30 días;Etapas estancadas;", csv, StringComparison.Ordinal);
        Assert.Contains("Tesis en curso por etapa;1. Estudiante;Tesis;", csv, StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.RegistrosAuditoria.AnyAsync(r => r.Accion == AccionesAuditoria.ReporteExportado, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Los_parametros_invalidos_devuelven_400()
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Kpis}?diasEstancamiento=0", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Cola}?separador=Tab", TestContext.Current.CancellationToken)).StatusCode);
    }
}
