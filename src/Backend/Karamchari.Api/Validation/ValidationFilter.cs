using FluentValidation;

namespace Karamchari.Api.Validation;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class ValidationFilter
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument == null) return await next(context);

            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator == null) return await next(context);

            var result = await validator.ValidateAsync(argument);
            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.ToDictionary());
            }

            return await next(context);
        });
    }
}
