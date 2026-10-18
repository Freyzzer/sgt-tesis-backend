using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Documentos;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Documentos;

public class DocumentosEndpointsTests : IClassFixture<ApiFactory>
{
    private const string Tesis = "/api/v1/tesis";

    private readonly ApiFactory _factory;

    public DocumentosEndpointsTests(ApiFactory factory) => _factory = factory;

    internal static byte[] Pdf(int relleno = 0) => Encoding.ASCII.GetBytes("%PDF-1.4\n% contenido de prueba\n" + new string('x', relleno) + "\n%%EOF");

    internal static byte[] Docx(bool valido = true)
    {
        using var memoria = new MemoryStream();

        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.CreateEntry("[Content_Types].xml").Open().Dispose();
            zip.CreateEntry(valido ? "word/document.xml" : "otra/cosa.xml").Open().Dispose();
        }

        return memoria.ToArray();
    }

    private static MultipartFormDataContent Formulario(byte[] contenido, string nombre, string categoria = "Propuesta firmada")
    {
        var form = new MultipartFormDataContent { { new StringContent(categoria), "categoria" } };
        form.Add(new ByteArrayContent(contenido), "archivo", nombre);

        return form;
    }

    private async Task<(Guid UsuarioId, HttpClient Cliente, Guid TesisId)> EstudianteConTesisAsync(ApiFactory? factory = null)
    {
        factory ??= _factory;
        var (usuarioId, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var cliente = await factory.ClienteComoAsync(correo);
        var respuesta = await cliente.PostAsJsonAsync(Tesis, new { titulo = "Titulo", resumen = "Resumen" });
        var tesisId = (await respuesta.LeerJsonAsync()).GetProperty("id").GetGuid();

        return (usuarioId, cliente, tesisId);
    }

    private static string Url(Guid tesisId, int etapa = 1) => $"{Tesis}/{tesisId}/etapas/{etapa}/documentos";

    [Fact]
    [Trait("AC", "7.1")]
    public async Task Se_aceptan_pdf_y_docx_validos()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        var pdf = await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "propuesta.pdf", "Propuesta"), TestContext.Current.CancellationToken);
        var docx = await cliente.PostAsync(Url(tesisId), Formulario(Docx(), "carta.docx", "Carta"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, pdf.StatusCode);
        Assert.Equal(TiposDeArchivo.Pdf, (await pdf.LeerJsonAsync()).GetProperty("contentType").GetString());
        Assert.Equal(HttpStatusCode.Created, docx.StatusCode);
        Assert.Equal(TiposDeArchivo.Docx, (await docx.LeerJsonAsync()).GetProperty("contentType").GetString());
    }

    [Theory]
    [Trait("AC", "7.1")]
    [InlineData("texto.pdf", "texto plano disfrazado")]
    [InlineData("programa.exe", "MZ-ejecutable")]
    [InlineData("hoja.xlsx", "PK")]
    [InlineData("sin-extension", "%PDF-1.4")]
    public async Task Archivos_con_extension_o_contenido_no_permitidos_devuelven_415(string nombre, string contenido)
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        var respuesta = await cliente.PostAsync(Url(tesisId), Formulario(Encoding.ASCII.GetBytes(contenido), nombre), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "7.1")]
    public async Task La_firma_debe_coincidir_con_la_extension_y_un_docx_debe_ser_un_docx_real()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "falso.docx"), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await cliente.PostAsync(Url(tesisId), Formulario(Docx(), "falso.pdf"), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await cliente.PostAsync(Url(tesisId), Formulario(Docx(valido: false), "zip.docx"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "7.2")]
    public async Task Un_archivo_mayor_al_maximo_configurado_devuelve_413()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Documentos:MaxBytes"] = "2048";
        var (_, cliente, tesisId) = await EstudianteConTesisAsync(factory);

        var justoEnElLimite = Pdf(2048 - Pdf().Length);
        Assert.Equal(2048, justoEnElLimite.Length);

        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsync(Url(tesisId), Formulario(justoEnElLimite, "ok.pdf"), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(2049), "grande.pdf"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "7.3")]
    public async Task El_archivo_se_guarda_con_nombre_guid_y_se_registra_su_hash_sha256()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();
        var contenido = Pdf(500);

        var respuesta = await cliente.PostAsync(Url(tesisId), Formulario(contenido, "Mi Propuesta Final.pdf"), TestContext.Current.CancellationToken);
        var body = await respuesta.LeerJsonAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var documento = await db.Documentos.SingleAsync(d => d.Id == body.GetProperty("id").GetGuid(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Mi Propuesta Final.pdf", documento.NombreOriginal);
        Assert.Matches("^[0-9a-f]{32}\\.pdf$", documento.NombreAlmacenado);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(contenido)), documento.HashSha256);
        Assert.Equal(contenido.Length, documento.TamanoBytes);

        var enDisco = Path.Combine(_factory.RutaAlmacen, documento.NombreAlmacenado);
        Assert.Equal(contenido, await File.ReadAllBytesAsync(enDisco, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(_factory.RutaAlmacen, "Mi Propuesta Final.pdf")));
    }

    [Fact]
    [Trait("AC", "7.3")]
    public async Task Un_nombre_con_ruta_se_reduce_al_nombre_de_archivo()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        var respuesta = await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "..\\..\\windows\\system32\\evil.pdf"), TestContext.Current.CancellationToken);
        var body = await respuesta.LeerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("evil.pdf", body.GetProperty("nombreOriginal").GetString());
    }

    [Fact]
    [Trait("AC", "7.3")]
    public async Task El_almacenamiento_rechaza_nombres_que_no_son_generados_por_el_sistema()
    {
        var storage = _factory.Services.GetRequiredService<IFileStorage>();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.GuardarAsync("../evil.pdf", new MemoryStream([1]), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.GuardarAsync("nombre-libre.pdf", new MemoryStream([1]), CancellationToken.None));
    }

    [Fact]
    [Trait("AC", "7.4")]
    public async Task Subir_la_misma_categoria_crea_versiones_y_conserva_las_anteriores()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        var v1 = await (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(1), "a.pdf", "Propuesta firmada"), TestContext.Current.CancellationToken)).LeerJsonAsync();
        var v2 = await (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(2), "b.pdf", "Propuesta firmada"), TestContext.Current.CancellationToken)).LeerJsonAsync();
        var v3 = await (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(3), "c.pdf", "  PROPUESTA FIRMADA "), TestContext.Current.CancellationToken)).LeerJsonAsync();
        var otra = await (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(4), "d.pdf", "Carta de aval"), TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal([1, 2, 3], new[] { v1, v2, v3 }.Select(v => v.GetProperty("version").GetInt32()));
        Assert.Equal(1, otra.GetProperty("version").GetInt32());

        var lista = await (await cliente.GetAsync(Url(tesisId), TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(4, lista.GetArrayLength());
        Assert.Equal(3, lista.EnumerateArray().Count(d => d.GetProperty("categoria").GetString()!.Trim().Equals("propuesta firmada", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    [Trait("AC", "7.5")]
    public async Task Una_etapa_en_revision_no_admite_documentos()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();
        await cliente.PostAsync($"{Tesis}/{tesisId}/etapas/1/enviar", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [Trait("AC", "7.5")]
    [InlineData(EstadoEtapa.Aprobado)]
    [InlineData(EstadoEtapa.Rechazado)]
    public async Task Una_etapa_aprobada_o_rechazada_devuelve_409(EstadoEtapa estado)
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var etapa = await db.Tesis.Where(t => t.Id == tesisId).SelectMany(t => t.Etapas).SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
            db.Entry(etapa).Property(nameof(Etapa.Estado)).CurrentValue = estado;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "7.1")]
    public async Task Solo_el_dueno_puede_subir_y_se_exige_archivo_y_categoria()
    {
        var (_, duenio, tesisId) = await EstudianteConTesisAsync();
        var (_, correoOtro) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var otro = await _factory.ClienteComoAsync(correoOtro);
        using var admin = await _factory.ClienteAdministradorAsync();
        using var anonimo = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await otro.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);

        var sinArchivo = new MultipartFormDataContent { { new StringContent("Propuesta"), "categoria" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await duenio.PostAsync(Url(tesisId), sinArchivo, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await duenio.PostAsync(Url(tesisId), Formulario(Pdf(), "a.pdf", ""), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await duenio.PostAsync(Url(tesisId, 9), Formulario(Pdf(), "a.pdf"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "7.1")]
    public async Task Un_archivo_vacio_devuelve_400()
    {
        var (_, cliente, tesisId) = await EstudianteConTesisAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync(Url(tesisId), Formulario([], "vacio.pdf"), TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "8.1")]
    public async Task Descargan_el_dueno_y_el_administrador_pero_no_otros()
    {
        var (_, duenio, tesisId) = await EstudianteConTesisAsync();
        var contenido = Pdf(300);
        var id = (await (await duenio.PostAsync(Url(tesisId), Formulario(contenido, "tesis.pdf"), TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();
        var (_, correoOtro) = await _factory.SembrarUsuarioAsync(Rol.Estudiante);
        using var otro = await _factory.ClienteComoAsync(correoOtro);
        var (_, correoEval) = await _factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await _factory.ClienteComoAsync(correoEval);
        using var admin = await _factory.ClienteAdministradorAsync();
        using var anonimo = _factory.CreateClient();
        var ruta = $"/api/v1/documentos/{id}/contenido";

        var propia = await duenio.GetAsync(ruta, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
        Assert.Equal(contenido, await propia.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await evaluador.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await duenio.GetAsync($"/api/v1/documentos/{Guid.NewGuid()}/contenido", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "8.2")]
    public async Task La_descarga_fuerza_attachment_y_nosniff()
    {
        var (_, duenio, tesisId) = await EstudianteConTesisAsync();
        var id = (await (await duenio.PostAsync(Url(tesisId), Formulario(Pdf(), "mi tesis.pdf"), TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("id").GetGuid();

        var respuesta = await duenio.GetAsync($"/api/v1/documentos/{id}/contenido", TestContext.Current.CancellationToken);

        Assert.Equal("attachment", respuesta.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("mi tesis.pdf", respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(TiposDeArchivo.Pdf, respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("AC", "6.2")]
    public async Task Enviar_sin_los_documentos_obligatorios_devuelve_422_con_la_lista_de_faltantes()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Documentos:Requisitos:Estudiante:0"] = "Propuesta firmada";
        factory.Configuracion["Documentos:Requisitos:Estudiante:1"] = "Carta de aval";
        var (_, cliente, tesisId) = await EstudianteConTesisAsync(factory);
        var enviar = $"{Tesis}/{tesisId}/etapas/1/enviar";

        var sinNada = await cliente.PostAsync(enviar, null, TestContext.Current.CancellationToken);
        var faltantes = (await sinNada.LeerJsonAsync()).GetProperty("faltantes").EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinNada.StatusCode);
        Assert.Equal(["Propuesta firmada", "Carta de aval"], faltantes);

        await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "p.pdf", "propuesta FIRMADA"), TestContext.Current.CancellationToken);
        var parcial = await cliente.PostAsync(enviar, null, TestContext.Current.CancellationToken);
        Assert.Equal(["Carta de aval"], (await parcial.LeerJsonAsync()).GetProperty("faltantes").EnumerateArray().Select(e => e.GetString()));

        await cliente.PostAsync(Url(tesisId), Formulario(Pdf(), "c.pdf", "Carta de aval"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync(enviar, null, TestContext.Current.CancellationToken)).StatusCode);
    }
}
