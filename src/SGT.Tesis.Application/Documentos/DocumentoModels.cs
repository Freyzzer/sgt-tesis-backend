using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Documentos;

public sealed record DocumentoDto(
    Guid Id,
    string Categoria,
    string NombreOriginal,
    string ContentType,
    long TamanoBytes,
    string HashSha256,
    int Version,
    DateTime CreatedAt)
{
    public static DocumentoDto Desde(Documento d) =>
        new(d.Id, d.Categoria, d.NombreOriginal, d.ContentType, d.TamanoBytes, d.HashSha256, d.Version, d.CreatedAt);
}

public interface IDocumentoRepository
{
    Task<int> UltimaVersionAsync(Guid etapaId, string categoriaClave, CancellationToken cancellationToken);

    Task<IReadOnlyList<Documento>> ListarPorEtapaAsync(Guid etapaId, CancellationToken cancellationToken);

    Task<Documento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken);

    void Agregar(Documento documento);
}

/// <summary>Almacén de contenido de archivos. Solo recibe nombres generados por el sistema.</summary>
public interface IFileStorage
{
    Task GuardarAsync(string nombreAlmacenado, Stream contenido, CancellationToken cancellationToken);

    Task<Stream> AbrirLecturaAsync(string nombreAlmacenado, CancellationToken cancellationToken);

    Task EliminarAsync(string nombreAlmacenado, CancellationToken cancellationToken);
}

/// <summary>Reglas de documentos configurables por ambiente.</summary>
public interface IDocumentosPolicy
{
    long TamanoMaximoBytes { get; }

    /// <summary>Categorías obligatorias de la etapa (vacío si el Product Owner aún no las definió).</summary>
    IReadOnlyList<string> CategoriasRequeridas(EtapaTipo tipo);
}
