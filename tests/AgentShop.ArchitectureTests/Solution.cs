using AgentShop.Modules.Catalog;
using AgentShop.Modules.Catalog.Contracts;
using AgentShop.Modules.Orders;
using AgentShop.Modules.Orders.Contracts;
using AgentShop.Modules.Shipping;
using AgentShop.Modules.Shipping.Contracts;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;
using ArchUnitNET.Loader;
using Architecture = ArchUnitNET.Domain.Architecture;
using Assembly = System.Reflection.Assembly;
using Type = System.Type;

namespace AgentShop.ArchitectureTests;

/// <summary>
/// The application under test, loaded once. Add new modules here (see .github/skills/add-module).
/// </summary>
internal static class Solution
{
    public static readonly IReadOnlyList<ModuleInfo> Modules =
    [
        new("Catalog", typeof(CatalogModule).Assembly, typeof(ICatalogModule).Assembly, typeof(CatalogModule)),
        new("Orders", typeof(OrdersModule).Assembly, typeof(OrderPlacedIntegrationEvent).Assembly, typeof(OrdersModule)),
        new("Shipping", typeof(ShippingModule).Assembly, typeof(ShipmentDispatchedIntegrationEvent).Assembly, typeof(ShippingModule)),
    ];

    public static readonly Assembly SharedKernel = typeof(Result).Assembly;

    public static readonly Assembly SharedKernelAspNetCore = typeof(IEndpoint).Assembly;

    public static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
        [
            SharedKernel,
            SharedKernelAspNetCore,
            .. Modules.SelectMany(m => new[] { m.Assembly, m.ContractsAssembly }),
        ])
        .Build();

    public static ModuleInfo Module(string name) => Modules.Single(m => m.Name == name);

    public static TheoryData<string> ModuleNames() => [.. Modules.Select(m => m.Name)];
}

internal sealed record ModuleInfo(string Name, Assembly Assembly, Assembly ContractsAssembly, Type ModuleType)
{
    public string Namespace => $"AgentShop.Modules.{Name}";

    public string ContractsNamespace => $"{Namespace}.Contracts";
}
