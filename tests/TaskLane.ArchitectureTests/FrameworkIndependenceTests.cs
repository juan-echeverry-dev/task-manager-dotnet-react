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
        "Microsoft.Extensions.Identity",
        "Microsoft.IdentityModel",
        "System.IdentityModel",
    ];

    // The Domain is plain C#: no validation or logging libraries either.
    private static readonly string[] DomainOnlyForbiddenAssemblyPrefixes =
    [
        "FluentValidation",
        "Microsoft.Extensions",
    ];

    [Fact]
    public void Domain_DoesNotReferenceInfrastructureFrameworks() =>
        ForbiddenReferencesOf(Layers.Domain, ForbiddenAssemblyPrefixes).ShouldBeEmpty();

    [Fact]
    public void Domain_DoesNotReferenceApplicationLibraries() =>
        ForbiddenReferencesOf(Layers.Domain, DomainOnlyForbiddenAssemblyPrefixes).ShouldBeEmpty();

    [Fact]
    public void Application_DoesNotReferenceInfrastructureFrameworks() =>
        ForbiddenReferencesOf(Layers.Application, ForbiddenAssemblyPrefixes).ShouldBeEmpty();

    private static string[] ForbiddenReferencesOf(Assembly assembly, string[] forbiddenPrefixes) =>
        [.. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.Ordinal)))];
}
