using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Philobiblos.Infrastructure.Filters;

public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (validator is not null && argument is not null)
        {
            var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => JsonNamingPolicy.CamelCase.ConvertName(group.Key),
                        group => group.Select(failure => failure.ErrorMessage).ToArray());

                return Results.ValidationProblem(
                    errors,
                    detail: "One or more validation errors occurred.",
                    title: "Bad Request");
            }
        }

        return await next(context);
    }
}
