using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TaskLane.ArchitectureTests;

/// <summary>
/// Modules interact only through public contracts. Tasks knows people only by their id (the
/// <c>sub</c> claim), never through Identity types.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api)
        .Build();

    // Infrastructure persistence is exempt: the Tasks table configuration declares the foreign key to
    // Users so the database keeps owners valid (research R11).
    private static readonly IObjectProvider<IType> TasksModule =
        Types().That().ResideInNamespaceMatching(@"^TaskLane\.(Domain|Application|Api)\.Tasks(\..+)?$")
            .As("Tasks module");

    private static readonly IObjectProvider<IType> IdentityModule =
        Types().That().ResideInNamespaceMatching(@"^TaskLane\.\w+\.Identity(\..+)?$")
            .As("Identity module");

    [Fact]
    public void Tasks_DoesNotDependOnIdentity() =>
        Types().That().Are(TasksModule)
            .Should().NotDependOnAny(IdentityModule)
            .Check(Architecture);
}
