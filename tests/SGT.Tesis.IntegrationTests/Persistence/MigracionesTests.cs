using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SGT.Tesis.Infrastructure.Persistence;
using SGT.Tesis.IntegrationTests.Support;

namespace SGT.Tesis.IntegrationTests.Persistence;

public class MigracionesTests
{
    private static AppDbContext ContextoSqlServer(string cadena) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cadena).Options);

    [Fact]
    [Trait("NFR", "16")]
    public void El_modelo_actual_no_tiene_cambios_pendientes_de_migrar()
    {
        // No abre conexión: compara el modelo del código con la última instantánea de migraciones.
        using var contexto = ContextoSqlServer("Server=(none);Database=modelo;Trusted_Connection=True;");

        Assert.False(
            contexto.Database.HasPendingModelChanges(),
            "El modelo cambió sin una migración: ejecute `dotnet ef migrations add <Nombre>`.");
    }

    [Fact]
    [Trait("NFR", "16")]
    public void Las_migraciones_estan_ordenadas_y_no_hay_duplicados()
    {
        using var contexto = ContextoSqlServer("Server=(none);Database=modelo;Trusted_Connection=True;");

        var migraciones = contexto.Database.GetMigrations().ToList();

        Assert.NotEmpty(migraciones);
        Assert.Equal(migraciones.Order(StringComparer.Ordinal).ToList(), migraciones);
        Assert.Equal(migraciones.Count, migraciones.Distinct().Count());
    }

    [Fact]
    [Trait("NFR", "16")]
    public async Task Las_migraciones_se_aplican_en_una_base_nueva_y_crean_los_indices_esperados()
    {
        Assert.SkipUnless(ApiFactory.UsaSqlServer, $"Requiere un SQL Server real: defina la variable {ApiFactory.VariableSqlServer}.");

        var servidor = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(ApiFactory.VariableSqlServer)!);
        var baseDeDatos = $"sgt_migraciones_{Guid.NewGuid():N}";
        servidor.InitialCatalog = baseDeDatos;

        try
        {
            await using (var contexto = ContextoSqlServer(servidor.ConnectionString))
            {
                await contexto.Database.MigrateAsync(TestContext.Current.CancellationToken);

                var aplicadas = (await contexto.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToList();
                Assert.Equal(contexto.Database.GetMigrations().ToList(), aplicadas);
                Assert.Empty(await contexto.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
            }

            await using var conexion = new SqlConnection(servidor.ConnectionString);
            await conexion.OpenAsync(TestContext.Current.CancellationToken);

            var tablas = await Consultar(conexion, "SELECT name FROM sys.tables");
            string[] esperadas =
            [
                "Usuarios", "RefreshTokens", "PasswordResetTokens", "Tesis", "Etapas", "Documentos", "AsignacionesEvaluador",
                "Veredictos", "Decisiones", "RegistrosAuditoria", "Notificaciones", "__EFMigrationsHistory",
            ];

            Assert.All(esperadas, tabla => Assert.Contains(tabla, tablas));

            // Índices únicos filtrados que garantizan reglas de negocio aun ante carreras.
            var filtrados = await Consultar(
                conexion,
                "SELECT name + ' | ' + filter_definition FROM sys.indexes WHERE is_unique = 1 AND has_filter = 1");

            Assert.Contains(filtrados, f => f.StartsWith("UX_Tesis_StudentId_EnCurso", StringComparison.Ordinal));
            Assert.Contains(filtrados, f => f.StartsWith("UX_Asignaciones_Etapa_Evaluador_Activa", StringComparison.Ordinal));

            // Sin cadenas de longitud ilimitada: ninguna columna de texto es nvarchar(max).
            var ilimitadas = await Consultar(
                conexion,
                "SELECT t.name + '.' + c.name FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id " +
                "JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE ty.name IN ('nvarchar','varchar') AND c.max_length = -1 AND t.name <> '__EFMigrationsHistory'");

            Assert.Empty(ilimitadas);
        }
        finally
        {
            await EliminarAsync(servidor, baseDeDatos);
        }
    }

    private static async Task<List<string>> Consultar(SqlConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        var resultado = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            resultado.Add(lector.GetString(0));
        }

        return resultado;
    }

    private static async Task EliminarAsync(SqlConnectionStringBuilder servidor, string baseDeDatos)
    {
        SqlConnection.ClearAllPools();
        servidor.InitialCatalog = "master";
        await using var conexion = new SqlConnection(servidor.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText =
            $"IF DB_ID(N'{baseDeDatos}') IS NOT NULL BEGIN ALTER DATABASE [{baseDeDatos}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{baseDeDatos}]; END";
        await comando.ExecuteNonQueryAsync();
    }
}
