using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Identidad;

/// <summary>
/// Token de un solo uso para restablecer la contraseña. Solo se guarda su hash; el valor original viaja
/// únicamente en el correo al usuario. Vence a los 30 minutos y se invalida al usarse o al pedir otro.
/// </summary>
public sealed class PasswordResetToken : BaseEntity
{
    public const int HashMaxLength = 128;
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    private PasswordResetToken()
    {
        TokenHash = string.Empty;
    }

    public Guid UsuarioId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiraEn { get; private set; }

    public DateTime? UsadoEn { get; private set; }

    public static PasswordResetToken Crear(Guid usuarioId, string tokenHash, DateTime utcNow) => new()
    {
        UsuarioId = Guard.NotEmpty(usuarioId, nameof(UsuarioId)),
        TokenHash = Guard.Required(tokenHash, HashMaxLength, nameof(TokenHash)),
        ExpiraEn = utcNow + Vigencia,
    };

    public bool EstaVigente(DateTime utcNow) => UsadoEn is null && ExpiraEn > utcNow;

    /// <summary>Marca el token como usado (o invalidado); es idempotente y conserva la primera fecha.</summary>
    public void MarcarUsado(DateTime utcNow) => UsadoEn ??= utcNow;
}
