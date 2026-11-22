namespace SGT.Tesis.Domain.Common;

internal static class Guard
{
    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} es obligatorio y debe ser un identificador válido.");
        }

        return value;
    }

    public static string Required(string? value, int maxLength, string name)
    {
        var text = value?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            throw new DomainException($"{name} es obligatorio.");
        }

        if (text.Length > maxLength)
        {
            throw new DomainException($"{name} no puede superar {maxLength} caracteres.");
        }

        return text;
    }
}
