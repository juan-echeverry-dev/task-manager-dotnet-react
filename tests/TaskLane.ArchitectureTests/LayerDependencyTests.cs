using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TaskLane.ArchitectureTests;

/// <summary>
/// Enforces the Clean Architecture dependency rule: Api → Infrastructure → Application → Domain.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(Layers.Domain).As("Domain layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(Layers.Application).As("Application layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(Layers.Infrastructure).As("Infrastructure layer");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(Layers.Api).As("Api layer");

    [Fact]
    public void Domain_DoesNotDependOnAnyOuterLayer() =>
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Check(Architecture);

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi() =>
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Check(Architecture);

    [Fact]
    public void Infrastructure_DoesNotDependOnApi() =>
        Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(ApiLayer)
            .Check(Architecture);
}
