using System.Text.RegularExpressions;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AgentShop.ArchitectureTests;

/// <summary>
/// Vertical slices are isolated: a slice may use Domain and Persistence of its module, never another slice.
/// </summary>
public sealed class SliceRules
{
    public static TheoryData<string, string> Slices()
    {
        var data = new TheoryData<string, string>();
        foreach (var module in Solution.Modules)
        {
            var sliceNamespaces = module.Assembly.GetTypes()
                .Select(t => t.Namespace)
                .OfType<string>()
                .Where(ns => ns.StartsWith($"{module.Namespace}.Features.", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);

            foreach (var ns in sliceNamespaces)
            {
                data.Add(module.Name, ns);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Slices))]
    public void Slice_DoesNotDependOn_OtherSlices(string moduleName, string sliceNamespace)
    {
        var module = Solution.Module(moduleName);
        var features = Regex.Escape($"{module.Namespace}.Features.");
        var otherSlices = $@"^{features}(?!{Regex.Escape(sliceNamespace[(module.Namespace.Length + ".Features.".Length)..])}$).+$";

        Types().That().ResideInNamespaceMatching($"^{Regex.Escape(sliceNamespace)}$")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(otherSlices)
            .Because("slices share only Domain/ and Persistence/; copy small glue code instead of coupling slices")
            .Check(Solution.Architecture);
    }
}
