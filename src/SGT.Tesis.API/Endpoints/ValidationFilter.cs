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

        var validador = context.HttpContext.RequestServices.GetService<IValidator<T>>();

        if (validador is not null)
        {
            var resultado = await validador.ValidateAsync(argumento, context.HttpContext.RequestAborted);

            if (!resultado.IsValid)
            {
                throw new ValidacionException(resultado.ToDictionary());
            }
        }

        return await next(context);
    }
}
