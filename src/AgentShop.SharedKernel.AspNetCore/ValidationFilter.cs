using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AgentShop.SharedKernel.AspNetCore;

/// <summary>
/// Validates the <typeparamref name="TRequest"/> argument with its FluentValidation validator before the handler runs.
/// A missing validator is a configuration error and throws; it never silently skips validation.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>()
            ?? throw new InvalidOperationException(
                $"No IValidator<{typeof(TRequest).FullName}> is registered. Add a nested Validator class to the slice and make sure the module calls AddValidatorsFromAssembly(..., includeInternalTypes: true).");

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Endpoint has no argument of type {typeof(TRequest).FullName} to validate.");

        var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary(), title: "One or more validation errors occurred.");
        }

        return await next(context);
    }
}
