using SGT.Tesis.Domain.Common;

namespace SGT.Tesis.Domain.GestionTesis;

/// <summary>Raíz de agregado del proceso de tesis de grado de un estudiante.</summary>
public sealed class TesisGrado : BaseEntity
{
    public const int TituloMaxLength = 300;
    public const int ResumenMaxLength = 3000;

    private TesisGrado()
    {
        Titulo = string.Empty;
        Resumen = string.Empty;
    }

    public Guid StudentId { get; private set; }

    public string Titulo { get; private set; }

    public string Resumen { get; private set; }

    public static TesisGrado Crear(Guid studentId, string? titulo, string? resumen) => new()
    {
        StudentId = Guard.NotEmpty(studentId, nameof(StudentId)),
        Titulo = Guard.Required(titulo, TituloMaxLength, nameof(Titulo)),
        Resumen = Guard.Required(resumen, ResumenMaxLength, nameof(Resumen)),
    };
}
