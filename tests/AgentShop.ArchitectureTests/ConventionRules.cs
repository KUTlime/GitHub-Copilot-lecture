using System.Runtime.CompilerServices;
using AgentShop.SharedKernel;
using AgentShop.SharedKernel.AspNetCore;

namespace AgentShop.ArchitectureTests;

/// <summary>
/// Visibility and naming conventions. Checked with reflection because "visible outside the assembly"
/// (Type.IsVisible / GetExportedTypes) is what matters, not the declared modifier of nested types.
/// </summary>
public sealed class ConventionRules
{
    private const string GeneratedMigrationsSuffix = ".Persistence.Migrations";

    public static TheoryData<string> Modules => Solution.ModuleNames();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_ExportsOnlyItsModuleClass(string moduleName)
    {
        var module = Solution.Module(moduleName);

        var exported = module.Assembly.GetExportedTypes()
            .Where(t => t.Namespace?.EndsWith(GeneratedMigrationsSuffix, StringComparison.Ordinal) != true)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(
            exported.SequenceEqual([module.ModuleType.FullName], StringComparer.Ordinal),
            $"Only {module.ModuleType.Name} may be public in {module.Assembly.GetName().Name} (EF migrations excepted). " +
            $"Make these types internal or move them to {module.ContractsNamespace}: {string.Join(", ", exported)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Endpoints_AreInternalSealedAndLiveInSlices(string moduleName)
    {
        var module = Solution.Module(moduleName);
        var endpoints = module.Assembly.GetTypes().Where(t => t.IsClass && t.IsAssignableTo(typeof(IEndpoint))).ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
        {
            Assert.True(endpoint.Namespace?.StartsWith($"{module.Namespace}.Features.", StringComparison.Ordinal) == true, $"{endpoint.FullName} must live in a slice under {module.Namespace}.Features.<Slice>.");
            Assert.True(endpoint.IsSealed, $"{endpoint.FullName} must be sealed.");
            Assert.False(endpoint.IsVisible, $"{endpoint.FullName} must not be visible outside the module.");
        });
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void IntegrationEvents_AreSealedRecordsInContracts(string moduleName)
    {
        var module = Solution.Module(moduleName);
        var events = module.ContractsAssembly.GetTypes().Where(t => t.IsClass && t.IsAssignableTo(typeof(IIntegrationEvent))).ToList();

        Assert.All(events, integrationEvent =>
        {
            Assert.Equal(module.ContractsNamespace, integrationEvent.Namespace);
            Assert.EndsWith("IntegrationEvent", integrationEvent.Name, StringComparison.Ordinal);
            Assert.True(integrationEvent.IsSealed, $"{integrationEvent.Name} must be sealed.");
            Assert.True(IsRecord(integrationEvent), $"{integrationEvent.Name} must be a record (immutable, value equality).");
        });

        var eventsOutsideContracts = module.Assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(IIntegrationEvent))).Select(t => t.FullName);
        Assert.Empty(eventsOutsideContracts);
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$") is not null
        && type.GetProperty("EqualityContract", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetMethod?.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) == true;
}
