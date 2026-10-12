using System.Text.RegularExpressions;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AgentShop.ArchitectureTests;

/// <summary>
/// Dependency rules between modules, contracts, domain and shared kernel.
/// If a rule fails, fix the code - do not change the rule without an explicit request.
/// </summary>
public sealed class DependencyRules
{
    private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\..*)?$";
    private const string AspNetCore = @"^Microsoft\.AspNetCore(\..*)?$";

    public static TheoryData<string> Modules => Solution.ModuleNames();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_DoesNotDependOn_InternalsOfOtherModules(string moduleName)
    {
        var module = Solution.Module(moduleName);
        var otherModules = Solution.Modules.Where(m => m != module).Select(m => Regex.Escape(m.Name));
        var otherModuleInternals = $@"^AgentShop\.Modules\.({string.Join('|', otherModules)})(\.(?!Contracts(\.|$)).*)?$";

        Types().That().ResideInAssembly(module.Assembly)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(otherModuleInternals)
            .Because("modules talk to each other only through *.Contracts (interfaces, DTOs, integration events)")
            .Check(Solution.Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Contracts_DoNotDependOn_EfCoreAspNetCoreOrModuleInternals(string moduleName)
    {
        var module = Solution.Module(moduleName);
        var anyModuleInternals = @"^AgentShop\.Modules\.[A-Za-z]+(\.(?!Contracts(\.|$)).*)?$";

        Types().That().ResideInAssembly(module.ContractsAssembly)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(EntityFrameworkCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AspNetCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(anyModuleInternals)
            .Because("contracts are the stable public API of a module and must stay free of infrastructure")
            .Check(Solution.Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_DoesNotDependOn_InfrastructureFeaturesOrPersistence(string moduleName)
    {
        var module = Solution.Module(moduleName);
        var ns = Regex.Escape(module.Namespace);

        Types().That().ResideInNamespaceMatching($@"^{ns}\.Domain$")
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(EntityFrameworkCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AspNetCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching($@"^{ns}\.(Features|Persistence)(\..*)?$")
            .Because("business rules live in the domain and must not know about HTTP, EF Core or slices")
            .Check(Solution.Architecture);
    }

    [Fact]
    public void SharedKernel_DoesNotDependOn_ModulesOrAspNetCore()
    {
        Types().That().ResideInAssembly(Solution.SharedKernel)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^AgentShop\.Modules(\..*)?$")
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AspNetCore)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(EntityFrameworkCore)
            .Because("the shared kernel is a pure leaf that every module may depend on")
            .Check(Solution.Architecture);
    }

    [Fact]
    public void SharedKernelAspNetCore_DoesNotDependOn_Modules()
    {
        Types().That().ResideInAssembly(Solution.SharedKernelAspNetCore)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^AgentShop\.Modules(\..*)?$")
            .Because("shared web plumbing must not know about any module")
            .Check(Solution.Architecture);
    }
}
