using FluentValidation;
using SGT.Tesis.Application.Common.Exceptions;

namespace SGT.Tesis.API.Endpoints;

/// <summary>Valida el argumento de tipo <typeparamref name="T"/> con FluentValidation antes de ejecutar el endpoint.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argumento = context.Arguments.OfType<T>().FirstOrDefault();

        if (argumento is null)
        {
            throw new ValidacionException(new Dictionary<string, string[]> { ["cuerpo"] = ["El cuerpo de la solicitud es obligatorio."] });
        }

        await ValidarAsync(argumento, context.HttpContext.RequestServices, context.HttpContext.RequestAborted);

        return await next(context);
    }

    /// <summary>Validación explícita para comandos armados a partir de la ruta y el cuerpo.</summary>
    public static async Task ValidarAsync(T argumento, IServiceProvider services, CancellationToken cancellationToken)
    {
        var validador = services.GetService<IValidator<T>>();

        if (validador is null)
        {
            return;
        }

        var resultado = await validador.ValidateAsync(argumento, cancellationToken);

        if (!resultado.IsValid)
        {
            throw new ValidacionException(resultado.ToDictionary());
        }
    }
}
