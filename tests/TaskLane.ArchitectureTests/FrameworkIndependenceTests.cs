using System.Reflection;

namespace TaskLane.ArchitectureTests;

/// <summary>
/// Business layers must stay independent of persistence and delivery frameworks.
/// </summary>
public sealed class FrameworkIndependenceTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "Microsoft.AspNetCore",
    ];

    [Fact]
    public void Domain_DoesNotReferenceInfrastructureFrameworks() =>
        ForbiddenReferencesOf(Layers.Domain).ShouldBeEmpty();

    [Fact]
    public void Application_DoesNotReferenceInfrastructureFrameworks() =>
        ForbiddenReferencesOf(Layers.Application).ShouldBeEmpty();

    private static string[] ForbiddenReferencesOf(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => ForbiddenAssemblyPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.Ordinal)))];
}
