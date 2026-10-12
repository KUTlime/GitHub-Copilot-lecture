using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace AgentShop.SharedKernel.AspNetCore;

/// <summary>
/// One HTTP endpoint of a vertical slice. Implementations are internal sealed classes nested in the slice.
/// Routes are RELATIVE to the module's route group (e.g. "/" or "/{id:guid}").
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps every <see cref="IEndpoint"/> declared in <paramref name="moduleAssembly"/> (internal types included)
    /// onto <paramref name="app"/>, typically the module's route group.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, Assembly moduleAssembly)
    {
        ArgumentNullException.ThrowIfNull(moduleAssembly);

        var endpointTypes = moduleAssembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)(Activator.CreateInstance(endpointType, nonPublic: true)
                ?? throw new InvalidOperationException($"Cannot create endpoint '{endpointType.FullName}'. Endpoints need a parameterless constructor."));
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
