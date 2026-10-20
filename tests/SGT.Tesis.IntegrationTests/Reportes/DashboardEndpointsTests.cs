using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Domain.Decisiones;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Reportes;

public class DashboardEndpointsTests
{
    private const string Kpis = "/api/v1/dashboard/kpis";
    private const string Cola = "/api/v1/dashboard/cola-validacion";

    private sealed record Estudiante(Guid TesisId, HttpClient Cliente);

    private static async Task<Estudiante> NuevaTesisAsync(ApiFactory factory, string titulo = "Titulo", bool enviar = true)
    {
        var (_, correo) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var cliente = await factory.ClienteComoAsync(correo);
        var tesisId = (await (await cliente.PostAsJsonAsync("/api/v1/tesis", new { titulo, resumen = "Resumen" })).LeerJsonAsync()).GetProperty("id").GetGuid();

        if (enviar)
        {
            await cliente.PostAsync($"/api/v1/tesis/{tesisId}/etapas/1/enviar", null);
        }

        return new Estudiante(tesisId, cliente);
    }

    private static Task<HttpResponseMessage> Enviar(Estudiante e) =>
        e.Cliente.PostAsync($"/api/v1/tesis/{e.TesisId}/etapas/1/enviar", null);

    private static Task<HttpResponseMessage> Decidir(HttpClient admin, Guid tesisId, string resultado, string? justificacion = null, int etapa = 1) =>
        admin.PostAsJsonAsync($"/api/v1/tesis/{tesisId}/etapas/{etapa}/decisiones", new { resultado, justificacion });

    // ---------- AC-14.1 ----------

    [Fact]
    [Trait("AC", "14.1")]
    public async Task Solo_el_administrador_accede_al_dashboard()
    {
        using var factory = new ApiFactory();
        var (_, correoEstudiante) = await factory.SembrarUsuarioAsync(Rol.Estudiante);
        var (_, correoEvaluador) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var estudiante = await factory.ClienteComoAsync(correoEstudiante);
        using var evaluador = await factory.ClienteComoAsync(correoEvaluador);
        using var anonimo = factory.CreateClient();

        foreach (var ruta in new[] { Kpis, Cola })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await evaluador.GetAsync(ruta, TestContext.Current.CancellationToken)).StatusCode);
        }
    }

    // ---------- AC-14.2: cola de validación ----------

    [Fact]
    [Trait("AC", "14.2")]
    public async Task La_cola_lista_las_etapas_en_revision_de_la_mas_antigua_a_la_mas_reciente()
    {
        using var factory = new ApiFactory();
        var inicio = DateTime.UtcNow;
        var primera = await NuevaTesisAsync(factory, "Primera", enviar: false);
        var segunda = await NuevaTesisAsync(factory, "Segunda", enviar: false);
        var tercera = await NuevaTesisAsync(factory, "Tercera", enviar: false);
        var sinEnviar = await NuevaTesisAsync(factory, "Sin enviar", enviar: false);
        using var admin = await factory.ClienteAdministradorAsync();

        // Los tokens se obtienen con el reloj real; después se mueve el reloj solo para fechar los envíos.
        // Orden de envío distinto al de creación: segunda (hace 10 días), tercera (hace 5), primera (hoy).
        factory.Reloj.UtcNow = inicio.AddDays(-10);
        await Enviar(segunda);
        factory.Reloj.UtcNow = inicio.AddDays(-5);
        await Enviar(tercera);
        factory.Reloj.UtcNow = inicio;
        await Enviar(primera);

        var pagina = await (await admin.GetAsync(Cola, TestContext.Current.CancellationToken)).LeerJsonAsync();
        var items = pagina.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(3, pagina.GetProperty("total").GetInt32());
        Assert.Equal(["Segunda", "Tercera", "Primera"], items.Select(i => i.GetProperty("titulo").GetString()));
        Assert.Equal([10, 5, 0], items.Select(i => i.GetProperty("diasEnRevision").GetInt32()));
        Assert.DoesNotContain(items, i => i.GetProperty("tesisId").GetGuid() == sinEnviar.TesisId);
        Assert.Equal("Ana Pérez", items[0].GetProperty("estudiante").GetString());
        Assert.Equal(1, items[0].GetProperty("etapa").GetInt32());
    }

    [Fact]
    [Trait("AC", "14.2")]
    public async Task La_cola_pagina_y_deja_de_incluir_las_etapas_ya_decididas()
    {
        using var factory = new ApiFactory();
        var tesis = new List<Estudiante>();

        for (var i = 0; i < 5; i++)
        {
            tesis.Add(await NuevaTesisAsync(factory, $"T{i}", enviar: false));
        }

        using var admin = await factory.ClienteAdministradorAsync();
        var inicio = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            factory.Reloj.UtcNow = inicio.AddSeconds(i * 10);
            await Enviar(tesis[i]);
        }

        factory.Reloj.UtcNow = inicio.AddMinutes(1);
        var p1 = await (await admin.GetAsync($"{Cola}?page=1&pageSize=2", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var p3 = await (await admin.GetAsync($"{Cola}?page=3&pageSize=2", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(5, p1.GetProperty("total").GetInt32());
        Assert.Equal(2, p1.GetProperty("items").GetArrayLength());
        Assert.Equal(1, p3.GetProperty("items").GetArrayLength());

        await Decidir(admin, tesis[0].TesisId, "Aprobada");
        await Decidir(admin, tesis[1].TesisId, "Observada", "Corregir.");

        var despues = await (await admin.GetAsync(Cola, TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(3, despues.GetProperty("total").GetInt32());
        Assert.Equal(["T2", "T3", "T4"], despues.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("titulo").GetString()));
    }

    [Fact]
    [Trait("AC", "14.2")]
    public async Task La_cola_muestra_cuantos_evaluadores_estan_asignados_y_cuantos_ya_emitieron_veredicto()
    {
        using var factory = new ApiFactory();
        var t = await NuevaTesisAsync(factory);
        var (id1, correo1) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var (id2, _) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var ev1 = await factory.ClienteComoAsync(correo1);
        using var admin = await factory.ClienteAdministradorAsync();
        var etapa = $"/api/v1/tesis/{t.TesisId}/etapas/1";

        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId = id1 }, cancellationToken: TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"{etapa}/evaluadores", new { evaluadorId = id2 }, cancellationToken: TestContext.Current.CancellationToken);
        await ev1.PutAsJsonAsync($"{etapa}/veredicto", new { concepto = "Favorable", comentarios = "Bien." }, cancellationToken: TestContext.Current.CancellationToken);

        var item = (await (await admin.GetAsync(Cola, TestContext.Current.CancellationToken)).LeerJsonAsync()).GetProperty("items")[0];

        Assert.Equal(2, item.GetProperty("evaluadoresAsignados").GetInt32());
        Assert.Equal(1, item.GetProperty("evaluadoresConVeredicto").GetInt32());
    }

    [Theory]
    [Trait("AC", "14.2")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task La_cola_rechaza_paginacion_invalida(string consulta)
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Cola}?{consulta}", TestContext.Current.CancellationToken)).StatusCode);
    }

    // ---------- KPIs ----------

    [Fact]
    [Trait("AC", "14.3")]
    public async Task Los_kpis_resumen_decisiones_tesis_por_etapa_y_tiempos()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Flujo:Etapas:Estudiante:PermiteReintento"] = "false"; // aquí el rechazo debe cerrar la tesis
        var inicio = DateTime.UtcNow;
        var aprobada = await NuevaTesisAsync(factory, enviar: false);
        var observada = await NuevaTesisAsync(factory, enviar: false);
        var rechazada = await NuevaTesisAsync(factory, enviar: false);
        var pendiente = await NuevaTesisAsync(factory, enviar: false);
        using var admin = await factory.ClienteAdministradorAsync();

        // Los tokens se obtienen con el reloj real; después se mueve el reloj solo para fechar los hechos.
        factory.Reloj.UtcNow = inicio;
        await Enviar(aprobada);
        await Enviar(observada);
        await Enviar(rechazada);

        factory.Reloj.UtcNow = inicio.AddDays(4);
        await Decidir(admin, aprobada.TesisId, "Aprobada");
        await Decidir(admin, observada.TesisId, "Observada", "Corregir.");
        factory.Reloj.UtcNow = inicio.AddDays(6);
        await Decidir(admin, rechazada.TesisId, "Rechazada", "No cumple.");

        factory.Reloj.UtcNow = inicio.AddDays(30);
        var k = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(4, k.GetProperty("totalTesis").GetInt32());
        Assert.Equal(3, k.GetProperty("tesisPorEstado").GetProperty("EnCurso").GetInt32());
        Assert.Equal(1, k.GetProperty("tesisPorEstado").GetProperty("Rechazada").GetInt32());
        Assert.Equal(0, k.GetProperty("tesisPorEstado").GetProperty("Finalizada").GetInt32());

        var porEtapa = k.GetProperty("tesisEnCursoPorEtapa").EnumerateArray().ToDictionary(e => e.GetProperty("numero").GetInt32(), e => e.GetProperty("cantidad").GetInt32());
        Assert.Equal(2, porEtapa[1]); // observada y pendiente siguen en la etapa 1
        Assert.Equal(1, porEtapa[2]); // la aprobada pasó a la etapa 2
        Assert.Equal(7, porEtapa.Count);

        var d = k.GetProperty("decisiones");
        Assert.Equal(3, d.GetProperty("total").GetInt32());
        Assert.Equal(1, d.GetProperty("aprobadas").GetInt32());
        Assert.Equal(33.3, d.GetProperty("tasaAprobacion").GetDouble());
        Assert.Equal(33.3, d.GetProperty("tasaObservacion").GetDouble());
        Assert.Equal(33.3, d.GetProperty("tasaRechazo").GetDouble());

        // Etapa 1 cerrada dos veces (aprobada a los 4 días, rechazada a los 6): promedio 5 días.
        var tiempo1 = k.GetProperty("tiempoPromedioPorEtapa")[0];
        Assert.Equal(2, tiempo1.GetProperty("etapasCerradas").GetInt32());
        Assert.Equal(5.0, tiempo1.GetProperty("promedioDias").GetDouble());
        Assert.Equal(0, k.GetProperty("tiempoPromedioPorEtapa")[1].GetProperty("etapasCerradas").GetInt32());

        _ = pendiente;
    }

    [Fact]
    [Trait("AC", "14.3")]
    public async Task Sin_datos_los_kpis_devuelven_ceros_sin_dividir_por_cero()
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();

        var k = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(0, k.GetProperty("totalTesis").GetInt32());
        Assert.Equal(0, k.GetProperty("decisiones").GetProperty("total").GetInt32());
        Assert.Equal(0.0, k.GetProperty("decisiones").GetProperty("tasaAprobacion").GetDouble());
        Assert.Equal(7, k.GetProperty("tesisEnCursoPorEtapa").GetArrayLength());
        Assert.Equal(0, k.GetProperty("etapasEnRevision").GetInt32());
        Assert.Equal(0, k.GetProperty("cargaPorEvaluador").GetArrayLength());
    }

    [Fact]
    [Trait("AC", "14.3")]
    public async Task Las_etapas_estancadas_dependen_del_umbral_de_dias()
    {
        using var factory = new ApiFactory();
        var inicio = DateTime.UtcNow;
        var vieja = await NuevaTesisAsync(factory, enviar: false);
        var reciente = await NuevaTesisAsync(factory, enviar: false);
        using var admin = await factory.ClienteAdministradorAsync();

        factory.Reloj.UtcNow = inicio;
        await Enviar(vieja);
        factory.Reloj.UtcNow = inicio.AddDays(30);
        await Enviar(reciente);
        factory.Reloj.UtcNow = inicio.AddDays(40);

        var por15 = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        var por5 = await (await admin.GetAsync($"{Kpis}?diasEstancamiento=5", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var por45 = await (await admin.GetAsync($"{Kpis}?diasEstancamiento=45", TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(2, por15.GetProperty("etapasEnRevision").GetInt32());
        Assert.Equal(15, por15.GetProperty("diasEstancamiento").GetInt32());
        Assert.Equal(1, por15.GetProperty("etapasEstancadas").GetInt32());
        Assert.Equal(2, por5.GetProperty("etapasEstancadas").GetInt32());
        Assert.Equal(0, por45.GetProperty("etapasEstancadas").GetInt32());
    }

    [Fact]
    [Trait("AC", "14.3")]
    public async Task Los_kpis_se_conservan_en_cache_el_tiempo_configurado_y_despues_se_recalculan()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Dashboard:KpisCacheSegundos"] = "30";
        var inicio = DateTime.UtcNow;
        factory.Reloj.UtcNow = inicio;
        await NuevaTesisAsync(factory, enviar: false);
        using var admin = await factory.ClienteAdministradorAsync();

        var primera = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        await NuevaTesisAsync(factory, enviar: false);

        factory.Reloj.UtcNow = inicio.AddSeconds(10);
        var enCache = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();

        factory.Reloj.UtcNow = inicio.AddSeconds(31);
        var recalculada = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();

        Assert.Equal(1, primera.GetProperty("totalTesis").GetInt32());
        Assert.Equal(1, enCache.GetProperty("totalTesis").GetInt32());
        Assert.Equal(primera.GetProperty("generadoEn").GetDateTime(), enCache.GetProperty("generadoEn").GetDateTime());
        Assert.Equal(2, recalculada.GetProperty("totalTesis").GetInt32());
    }

    [Fact]
    [Trait("AC", "14.3")]
    public async Task La_cache_distingue_el_umbral_de_estancamiento_y_la_cola_nunca_se_cachea()
    {
        using var factory = new ApiFactory();
        factory.Configuracion["Dashboard:KpisCacheSegundos"] = "60";
        var t = await NuevaTesisAsync(factory, enviar: false);
        using var admin = await factory.ClienteAdministradorAsync();

        var por15 = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        var por30 = await (await admin.GetAsync($"{Kpis}?diasEstancamiento=30", TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(15, por15.GetProperty("diasEstancamiento").GetInt32());
        Assert.Equal(30, por30.GetProperty("diasEstancamiento").GetInt32());

        // La cola refleja el cambio de inmediato aunque los KPIs sigan en caché.
        await Enviar(t);
        var cola = await (await admin.GetAsync(Cola, TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(1, cola.GetProperty("total").GetInt32());
    }

    [Theory]
    [Trait("AC", "14.3")]
    [InlineData(0)]
    [InlineData(366)]
    public async Task El_umbral_de_estancamiento_debe_estar_entre_1_y_365(int dias)
    {
        using var factory = new ApiFactory();
        using var admin = await factory.ClienteAdministradorAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Kpis}?diasEstancamiento={dias}", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    [Trait("AC", "14.3")]
    public async Task La_carga_por_evaluador_cuenta_asignaciones_activas_y_veredictos_pendientes()
    {
        using var factory = new ApiFactory();
        var t1 = await NuevaTesisAsync(factory);
        var t2 = await NuevaTesisAsync(factory);
        var (idEvaluador, correo) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        var (idLibre, _) = await factory.SembrarUsuarioAsync(Rol.Evaluador);
        using var evaluador = await factory.ClienteComoAsync(correo);
        using var admin = await factory.ClienteAdministradorAsync();

        await admin.PostAsJsonAsync($"/api/v1/tesis/{t1.TesisId}/etapas/1/evaluadores", new { evaluadorId = idEvaluador }, cancellationToken: TestContext.Current.CancellationToken);
        await admin.PostAsJsonAsync($"/api/v1/tesis/{t2.TesisId}/etapas/1/evaluadores", new { evaluadorId = idEvaluador }, cancellationToken: TestContext.Current.CancellationToken);
        await evaluador.PutAsJsonAsync($"/api/v1/tesis/{t1.TesisId}/etapas/1/veredicto", new { concepto = "Favorable", comentarios = "Ok." }, cancellationToken: TestContext.Current.CancellationToken);

        var k = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        var carga = k.GetProperty("cargaPorEvaluador").EnumerateArray().ToList();

        var fila = Assert.Single(carga);
        Assert.Equal(idEvaluador, fila.GetProperty("evaluadorId").GetGuid());
        Assert.Equal(2, fila.GetProperty("asignacionesActivas").GetInt32());
        Assert.Equal(1, fila.GetProperty("veredictosPendientes").GetInt32());
        Assert.DoesNotContain(carga, c => c.GetProperty("evaluadorId").GetGuid() == idLibre);

        // Al decidir una etapa deja de contar como carga.
        await Decidir(admin, t1.TesisId, "Aprobada");
        var despues = await (await admin.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        Assert.Equal(1, despues.GetProperty("cargaPorEvaluador")[0].GetProperty("asignacionesActivas").GetInt32());
    }

    // ---------- Volumen ----------

    [Fact]
    [Trait("AC", "14.3")]
    public async Task Con_10_000_tesis_los_kpis_y_la_cola_responden_con_rapidez()
    {
        using var factory = new ApiFactory();
        const int enRevision = 2_000;
        const int rechazadas = 8_000;
        var ahora = DateTime.UtcNow;
        var usuarios = new List<Usuario>();
        var tesis = new List<TesisGrado>();

        for (var i = 0; i < enRevision; i++)
        {
            var estudiante = Usuario.Crear($"Est{i}", "Masivo", $"masivo{i}@institucion.edu", "hash-de-prueba", Rol.Estudiante);
            usuarios.Add(estudiante);
            var t = TesisGrado.Crear(estudiante.Id, $"Tesis {i}", "Resumen", ahora.AddDays(-30));
            t.EnviarEtapaARevision(EtapaTipo.Estudiante, ahora.AddDays(-(i % 40)));
            tesis.Add(t);
        }

        var admin = usuarios[0].Id;
        var unEstudiante = usuarios[1].Id;
        var decisiones = new List<Decision>();

        for (var i = 0; i < rechazadas; i++)
        {
            var t = TesisGrado.Crear(unEstudiante, $"Rechazada {i}", "Resumen", ahora.AddDays(-60));
            t.EnviarEtapaARevision(EtapaTipo.Estudiante, ahora.AddDays(-50));
            decisiones.Add(t.Decidir(EtapaTipo.Estudiante, ResultadoDecision.Rechazada, admin, "No cumple.", ahora.AddDays(-45), new PoliticaEtapa(PermiteReintento: false, MaxEvaluadores: 3, ExigeUnanimidad: true)));
            tesis.Add(t);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.Usuarios.AddRange(usuarios);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            foreach (var lote in tesis.Chunk(1_000))
            {
                db.Tesis.AddRange(lote);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.ChangeTracker.Clear();
            }

            foreach (var lote in decisiones.Chunk(1_000))
            {
                db.Decisiones.AddRange(lote);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.ChangeTracker.Clear();
            }
        }

        using var cliente = await factory.ClienteAdministradorAsync();
        await cliente.GetAsync(Kpis, TestContext.Current.CancellationToken); // calienta el plan de consultas y el JIT

        var reloj = Stopwatch.StartNew();
        var k = await (await cliente.GetAsync(Kpis, TestContext.Current.CancellationToken)).LeerJsonAsync();
        var tiempoKpis = reloj.ElapsedMilliseconds;

        reloj.Restart();
        var cola = await (await cliente.GetAsync($"{Cola}?pageSize=100", TestContext.Current.CancellationToken)).LeerJsonAsync();
        var tiempoCola = reloj.ElapsedMilliseconds;

        Assert.Equal(enRevision + rechazadas, k.GetProperty("totalTesis").GetInt32());
        Assert.Equal(enRevision, k.GetProperty("etapasEnRevision").GetInt32());
        Assert.Equal(rechazadas, k.GetProperty("decisiones").GetProperty("rechazadas").GetInt32());
        Assert.Equal(enRevision, cola.GetProperty("total").GetInt32());
        Assert.Equal(100, cola.GetProperty("items").GetArrayLength());

        // Humo de rendimiento sobre SQLite en memoria; el p95 real (< 800 ms) debe medirse contra SQL Server.
        Assert.True(tiempoKpis < 2_000, $"KPIs: {tiempoKpis} ms");
        Assert.True(tiempoCola < 2_000, $"Cola: {tiempoCola} ms");
    }
}
