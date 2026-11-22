using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace SGT.Tesis.Infrastructure.Persistence;

/// <summary>
/// Parámetros de acceso a la base de datos. La cadena de conexión proviene de
/// <c>ConnectionStrings:DefaultConnection</c> (variable de entorno o gestor de secretos);
/// nunca se versiona con credenciales reales.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "DefaultConnection";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de ejecución de un comando, en segundos.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Reintentos ante fallas transitorias (por ejemplo, una caída momentánea de red).</summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>Espera máxima entre reintentos, en segundos.</summary>
    public int MaxRetryDelaySeconds { get; set; } = 10;
}

internal sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    private static readonly string[] PrivilegedLogins = ["sa", "dbo", "admin", "administrator"];

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            errors.Add(
                $"Falta la cadena de conexión 'ConnectionStrings:{DatabaseOptions.ConnectionStringName}'. " +
                "Configúrela por variable de entorno (ConnectionStrings__DefaultConnection) o user-secrets.");
        }
        else
        {
            ValidateConnectionString(options.ConnectionString, errors);
        }

        if (options.CommandTimeoutSeconds is < 5 or > 300)
        {
            errors.Add("Database:CommandTimeoutSeconds debe estar entre 5 y 300.");
        }

        if (options.MaxRetryCount is < 0 or > 10)
        {
            errors.Add("Database:MaxRetryCount debe estar entre 0 y 10.");
        }

        if (options.MaxRetryDelaySeconds is < 1 or > 60)
        {
            errors.Add("Database:MaxRetryDelaySeconds debe estar entre 1 y 60.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateConnectionString(string connectionString, List<string> errors)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (PrivilegedLogins.Contains(builder.UserID, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add("La aplicación no debe conectarse con una cuenta administrativa (sa/dbo/admin). Use el usuario de mínimo privilegio.");
            }
        }
        catch (ArgumentException)
        {
            errors.Add("La cadena de conexión no tiene un formato válido.");
        }
    }
}
