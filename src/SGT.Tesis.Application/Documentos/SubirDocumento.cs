using System.Security.Cryptography;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Application.Common.Exceptions;
using SGT.Tesis.Application.GestionTesis;
using SGT.Tesis.Domain.GestionTesis;

namespace SGT.Tesis.Application.Documentos;

public sealed record SubirDocumentoCommand(Guid TesisId, int Etapa, string Categoria, string NombreArchivo, Stream Contenido);

public sealed class SubirDocumentoCommandValidator : AbstractValidator<SubirDocumentoCommand>
{
    public SubirDocumentoCommandValidator()
    {
        RuleFor(x => x.Etapa).Must(n => Enum.IsDefined((EtapaTipo)n)).WithMessage("La etapa debe estar entre 1 y 7.");
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(Documento.CategoriaMaxLength);
        RuleFor(x => x.NombreArchivo).NotEmpty();
        RuleFor(x => x.Contenido).NotNull().WithMessage("El archivo es obligatorio.");
    }
}

public sealed class SubirDocumentoHandler(
    ITesisRepository tesis,
    IDocumentoRepository documentos,
    IFileStorage storage,
    IDocumentosPolicy politica,
    ICurrentUser actual,
    IUnitOfWork unitOfWork,
    ILogger<SubirDocumentoHandler> logger) : ICommandHandler<SubirDocumentoCommand, DocumentoDto>
{
    public async Task<DocumentoDto> HandleAsync(SubirDocumentoCommand command, CancellationToken cancellationToken)
    {
        AutorizacionTesis.ExigirEstudiante(actual);

        var propia = await tesis.ObtenerPorIdAsync(command.TesisId, cancellationToken)
            ?? throw new NoEncontradoException("La tesis no existe.");

        AutorizacionTesis.ExigirVisible(propia, actual);

        var etapa = propia.ExigirEtapaEditable((EtapaTipo)command.Etapa);

        var nombre = LimpiarNombre(command.NombreArchivo);
        var extension = Path.GetExtension(nombre).ToLowerInvariant();

        if (TiposDeArchivo.ExtensionPara(TiposDeArchivo.Pdf) != extension && TiposDeArchivo.ExtensionPara(TiposDeArchivo.Docx) != extension)
        {
            throw new TipoArchivoNoPermitidoException("Solo se permiten archivos PDF o DOCX.");
        }

        await using var contenido = await HacerSeekableAsync(command.Contenido, politica.TamanoMaximoBytes, cancellationToken);

        if (contenido.Length == 0)
        {
            throw new ValidacionException(new Dictionary<string, string[]> { ["archivo"] = ["El archivo está vacío."] });
        }

        var contentType = TiposDeArchivo.Detectar(contenido, extension)
            ?? throw new TipoArchivoNoPermitidoException("El contenido del archivo no corresponde a un PDF o DOCX válido.");

        var hash = Convert.ToHexString(await SHA256.HashDataAsync(contenido, cancellationToken));
        contenido.Position = 0;

        var version = await documentos.UltimaVersionAsync(etapa.Id, Documento.ClaveDeCategoria(command.Categoria), cancellationToken) + 1;
        var nombreAlmacenado = $"{Guid.NewGuid():N}{extension}";

        var documento = Documento.Crear(etapa.Id, command.Categoria, nombre, nombreAlmacenado, contentType, contenido.Length, hash, version);

        await storage.GuardarAsync(nombreAlmacenado, contenido, cancellationToken);

        try
        {
            documentos.Agregar(documento);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // No dejar archivos huérfanos si falla el registro en la base de datos.
            await storage.EliminarAsync(nombreAlmacenado, CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Documento {DocumentoId} (v{Version}, {Categoria}) cargado en la etapa {Etapa} de la tesis {TesisId} por {ActorId}",
            documento.Id,
            version,
            documento.Categoria,
            etapa.Tipo,
            propia.Id,
            actual.UserId);

        return DocumentoDto.Desde(documento);
    }

    private static string LimpiarNombre(string nombre)
    {
        var simple = Path.GetFileName(nombre.Replace('\\', '/'));
        var limpio = new string(simple.Where(c => !char.IsControl(c)).ToArray()).Trim();

        if (limpio.Length == 0)
        {
            throw new ValidacionException(new Dictionary<string, string[]> { ["archivo"] = ["El nombre del archivo no es válido."] });
        }

        return limpio.Length > Documento.NombreMaxLength ? limpio[^Documento.NombreMaxLength..] : limpio;
    }

    /// <summary>Entrega un flujo seekable y verifica el tamaño máximo sin leer más de lo necesario.</summary>
    private static async Task<Stream> HacerSeekableAsync(Stream origen, long maximo, CancellationToken cancellationToken)
    {
        if (origen is { CanSeek: true, Length: var longitud })
        {
            if (longitud > maximo)
            {
                throw new ArchivoDemasiadoGrandeException(maximo);
            }

            origen.Position = 0;
            return new NoCierraOrigenStream(origen);
        }

        var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;

        while ((leidos = await origen.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memoria.Length + leidos > maximo)
            {
                await memoria.DisposeAsync();
                throw new ArchivoDemasiadoGrandeException(maximo);
            }

            memoria.Write(buffer, 0, leidos);
        }

        memoria.Position = 0;
        return memoria;
    }

    /// <summary>Envoltura que impide cerrar el flujo del llamador al liberar el contenido.</summary>
    private sealed class NoCierraOrigenStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // No se libera el flujo original.
        }
    }
}
