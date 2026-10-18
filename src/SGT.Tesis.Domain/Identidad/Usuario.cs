using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.Identidad;

public sealed class Usuario : BaseEntity
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int CorreoMaxLength = 254;
    public const int PasswordHashMaxLength = 512;

    /// <summary>Intentos fallidos consecutivos que provocan el bloqueo temporal.</summary>
    public const int MaxIntentosFallidos = 5;

    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private Usuario()
    {
        Nombres = string.Empty;
        Apellidos = string.Empty;
        Correo = string.Empty;
        PasswordHash = string.Empty;
    }

    public string Nombres { get; private set; }

    public string Apellidos { get; private set; }

    /// <summary>Correo normalizado a minúsculas; único en el sistema.</summary>
    public string Correo { get; private set; }

    public string PasswordHash { get; private set; }

    public Rol Rol { get; private set; }

    public bool Activo { get; private set; }

    public int IntentosFallidos { get; private set; }

    public DateTime? BloqueadoHasta { get; private set; }

    public static string NormalizarCorreo(string? correo) =>
        Guard.Required(correo, CorreoMaxLength, nameof(Correo)).ToLowerInvariant();

    public static Usuario Crear(string? nombres, string? apellidos, string? correo, string passwordHash, Rol rol)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new DomainException("El rol indicado no es válido.");
        }

        var normalizado = NormalizarCorreo(correo);

        if (!normalizado.Contains('@') || normalizado.StartsWith('@') || normalizado.EndsWith('@'))
        {
            throw new DomainException("Correo no tiene un formato válido.");
        }

        return new Usuario
        {
            Nombres = Guard.Required(nombres, NombresMaxLength, nameof(Nombres)),
            Apellidos = Guard.Required(apellidos, ApellidosMaxLength, nameof(Apellidos)),
            Correo = normalizado,
            PasswordHash = Guard.Required(passwordHash, PasswordHashMaxLength, nameof(PasswordHash)),
            Rol = rol,
            Activo = true,
        };
    }

    public bool EstaBloqueado(DateTime utcNow) => BloqueadoHasta is not null && BloqueadoHasta > utcNow;

    /// <summary>Registra un intento fallido; al alcanzar el máximo bloquea la cuenta temporalmente.</summary>
    /// <returns><c>true</c> si este intento provocó el bloqueo.</returns>
    public bool RegistrarFalloDeAcceso(DateTime utcNow)
    {
        if (BloqueadoHasta is not null && BloqueadoHasta <= utcNow)
        {
            IntentosFallidos = 0;
            BloqueadoHasta = null;
        }

        IntentosFallidos++;

        if (IntentosFallidos < MaxIntentosFallidos)
        {
            return false;
        }

        BloqueadoHasta = utcNow.Add(DuracionBloqueo);
        IntentosFallidos = 0;
        return true;
    }

    public void RegistrarAccesoExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;

    public void CambiarRol(Rol nuevoRol)
    {
        if (!Enum.IsDefined(nuevoRol))
        {
            throw new DomainException("El rol indicado no es válido.");
        }

        Rol = nuevoRol;
    }
}
