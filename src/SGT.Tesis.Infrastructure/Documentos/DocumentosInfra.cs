using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.Documentos;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.Infrastructure.Documentos;

public sealed class DocumentosOptions
{
    public const string SectionName = "Documentos";
    public const long MaximoPermitidoBytes = 100L * 1024 * 1024;

    public long MaxBytes { get; set; } = 20L * 1024 * 1024;

    /// <summary>Categorías obligatorias por etapa (clave: nombre de la etapa). Por defecto (appsettings.json): la propuesta en «Propuesta» y la pasantía en «Pasantía».</summary>
    public Dictionary<string, string[]> Requisitos { get; set; } = [];
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Carpeta raíz de los archivos cargados. Debe estar fuera del repositorio y de la carpeta web.</summary>
    public string RootPath { get; set; } = string.Empty;
}

internal sealed class DocumentosOptionsValidator : IValidateOptions<DocumentosOptions>
{
    public ValidateOptionsResult Validate(string? name, DocumentosOptions options)
    {
        var errors = new List<string>();

        if (options.MaxBytes is < 1024 or > DocumentosOptions.MaximoPermitidoBytes)
        {
            errors.Add("Documentos:MaxBytes debe estar entre 1 KB y 100 MB.");
        }

        foreach (var etapa in options.Requisitos.Keys.Where(k => !Enum.TryParse<EtapaTipo>(k, out _)))
        {
            errors.Add($"Documentos:Requisitos:{etapa} no es una etapa válida ({string.Join(", ", Enum.GetNames<EtapaTipo>())}).");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

internal sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options) =>
        string.IsNullOrWhiteSpace(options.RootPath)
            ? ValidateOptionsResult.Fail("Falta Storage:RootPath (carpeta de archivos). Configúrela por variable de entorno (Storage__RootPath) o user-secrets.")
            : ValidateOptionsResult.Success;
}

internal sealed class DocumentosPolicy(IOptions<DocumentosOptions> options) : IDocumentosPolicy
{
    public long TamanoMaximoBytes => options.Value.MaxBytes;

    // Una entrada vacía anula el requisito por defecto (p. ej. Documentos__Requisitos__Propuesta__0="" en un ambiente de pruebas).
    public IReadOnlyList<string> CategoriasRequeridas(EtapaTipo tipo) =>
        options.Value.Requisitos.TryGetValue(tipo.ToString(), out var categorias)
            ? categorias.Where(c => !string.IsNullOrWhiteSpace(c)).ToList()
            : [];
}

/// <summary>Almacenamiento en disco. Solo acepta nombres generados por el sistema (GUID + extensión permitida).</summary>
internal sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _raiz;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _raiz = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_raiz);
    }

    [GeneratedRegex("^[0-9a-f]{32}\\.(pdf|docx)$")]
    private static partial Regex NombreValido();

    private string Ruta(string nombreAlmacenado)
    {
        if (!NombreValido().IsMatch(nombreAlmacenado))
        {
            throw new ArgumentException("Nombre de archivo no válido.", nameof(nombreAlmacenado));
        }

        var ruta = Path.GetFullPath(Path.Combine(_raiz, nombreAlmacenado));

        if (!ruta.StartsWith(_raiz, StringComparison.Ordinal))
        {
            throw new ArgumentException("Ruta fuera del almacenamiento.", nameof(nombreAlmacenado));
        }

        return ruta;
    }

    public async Task GuardarAsync(string nombreAlmacenado, Stream contenido, CancellationToken cancellationToken)
    {
        await using var destino = new FileStream(Ruta(nombreAlmacenado), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await contenido.CopyToAsync(destino, cancellationToken);
    }

    public Task<Stream> AbrirLecturaAsync(string nombreAlmacenado, CancellationToken cancellationToken)
    {
        var ruta = Ruta(nombreAlmacenado);

        if (!File.Exists(ruta))
        {
            throw new NoEncontradoException("El contenido del documento no está disponible.");
        }

        return Task.FromResult<Stream>(new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task EliminarAsync(string nombreAlmacenado, CancellationToken cancellationToken)
    {
        File.Delete(Ruta(nombreAlmacenado));
        return Task.CompletedTask;
    }
}

internal sealed class DocumentoRepository(AppDbContext db) : IDocumentoRepository
{
    public async Task<int> UltimaVersionAsync(Guid etapaId, string categoriaClave, CancellationToken cancellationToken) =>
        await db.Documentos
            .Where(d => d.EtapaId == etapaId && d.CategoriaClave == categoriaClave)
            .Select(d => (int?)d.Version)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task<IReadOnlyList<Documento>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken) =>
        await db.Documentos.AsNoTracking()
            .Where(d => d.EtapaId == etapaId)
            .OrderBy(d => d.CategoriaClave).ThenByDescending(d => d.Version)
            .ToListAsync(cancellationToken);

    public Task<Documento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Agregar(Documento documento) => db.Documentos.Add(documento);
}

/// <summary>Una etapa solo puede enviarse si cada categoría obligatoria tiene al menos una versión cargada.</summary>
internal sealed class RequisitosDocumentalesConfigurables(IDocumentosPolicy politica, IDocumentoRepository documentos) : IRequisitosDocumentales
{
    public async Task<IReadOnlyList<string>> ObtenerFaltantesAsync(Guid etapaId, EtapaTipo tipo, CancellationToken cancellationToken)
    {
        var requeridas = politica.CategoriasRequeridas(tipo);

        if (requeridas.Count == 0)
        {
            return [];
        }

        var cargadas = (await documentos.ListarPorEtapaAsync(etapaId, cancellationToken)).Select(d => d.CategoriaClave).ToHashSet();

        return requeridas.Where(r => !cargadas.Contains(Documento.ClaveDeCategoria(r))).ToList();
    }
}
