using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Identidad;

/// <summary>
/// Token de renovación de sesión. Solo se persiste su hash. Cada uso lo reemplaza por uno nuevo
/// (rotación); la reutilización de uno ya usado revoca toda la cadena (<see cref="FamiliaId"/>).
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    public const int HashMaxLength = 128;

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public Guid UsuarioId { get; private set; }

    public Guid FamiliaId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiraEn { get; private set; }

    public DateTime? RevocadoEn { get; private set; }

    public Guid? ReemplazadoPorId { get; private set; }

    public static RefreshToken Crear(Guid usuarioId, string tokenHash, DateTime expiraEn, Guid? familiaId = null) => new()
    {
        UsuarioId = usuarioId == Guid.Empty ? throw new DomainException("UsuarioId es obligatorio.") : usuarioId,
        FamiliaId = familiaId ?? Guid.CreateVersion7(),
        TokenHash = Guard.Required(tokenHash, HashMaxLength, nameof(TokenHash)),
        ExpiraEn = expiraEn,
    };

    public bool EstaRevocado => RevocadoEn is not null;

    public bool EstaActivo(DateTime utcNow) => RevocadoEn is null && ExpiraEn > utcNow;

    public void Revocar(DateTime utcNow, Guid? reemplazadoPorId = null)
    {
        RevocadoEn ??= utcNow;
        ReemplazadoPorId ??= reemplazadoPorId;
    }
}
