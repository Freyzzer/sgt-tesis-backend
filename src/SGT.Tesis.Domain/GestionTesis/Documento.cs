using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>
/// Metadatos de un archivo cargado en una etapa. El contenido vive en el almacenamiento de archivos.
/// Cada carga de una misma categoría crea una nueva versión; las anteriores se conservan.
/// </summary>
public sealed class Documento : BaseEntity
{
    public const int CategoriaMaxLength = 100;
    public const int NombreMaxLength = 255;
    public const int NombreAlmacenadoMaxLength = 100;
    public const int ContentTypeMaxLength = 100;
    public const int HashLength = 64;

    private Documento()
    {
        Categoria = string.Empty;
        CategoriaClave = string.Empty;
        NombreOriginal = string.Empty;
        NombreAlmacenado = string.Empty;
        ContentType = string.Empty;
        HashSha256 = string.Empty;
    }

    public Guid EtapaId { get; private set; }

    public string Categoria { get; private set; }

    /// <summary>Categoría normalizada (minúsculas, sin espacios sobrantes) usada para agrupar versiones.</summary>
    public string CategoriaClave { get; private set; }

    public string NombreOriginal { get; private set; }

    /// <summary>Nombre en el almacenamiento: un GUID más la extensión; nunca el nombre enviado por el usuario.</summary>
    public string NombreAlmacenado { get; private set; }

    public string ContentType { get; private set; }

    public long TamanoBytes { get; private set; }

    public string HashSha256 { get; private set; }

    public int Version { get; private set; }

    public static string ClaveDeCategoria(string? categoria) =>
        Guard.Required(categoria, CategoriaMaxLength, nameof(Categoria)).ToLowerInvariant();

    public static Documento Crear(
        Guid etapaId,
        string? categoria,
        string? nombreOriginal,
        string? nombreAlmacenado,
        string? contentType,
        long tamanoBytes,
        string? hashSha256,
        int version)
    {
        if (tamanoBytes <= 0)
        {
            throw new DomainException("El archivo está vacío.");
        }

        if (version < 1)
        {
            throw new DomainException("La versión debe ser mayor o igual a 1.");
        }

        var hash = Guard.Required(hashSha256, HashLength, nameof(HashSha256));

        if (hash.Length != HashLength)
        {
            throw new DomainException($"{nameof(HashSha256)} debe tener {HashLength} caracteres.");
        }

        return new Documento
        {
            EtapaId = Guard.NotEmpty(etapaId, nameof(EtapaId)),
            Categoria = Guard.Required(categoria, CategoriaMaxLength, nameof(Categoria)),
            CategoriaClave = ClaveDeCategoria(categoria),
            NombreOriginal = Guard.Required(nombreOriginal, NombreMaxLength, nameof(NombreOriginal)),
            NombreAlmacenado = Guard.Required(nombreAlmacenado, NombreAlmacenadoMaxLength, nameof(NombreAlmacenado)),
            ContentType = Guard.Required(contentType, ContentTypeMaxLength, nameof(ContentType)),
            TamanoBytes = tamanoBytes,
            HashSha256 = hash.ToUpperInvariant(),
            Version = version,
        };
    }
}
