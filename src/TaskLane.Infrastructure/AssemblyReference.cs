using System.Reflection;

namespace TaskLane.Infrastructure;

/// <summary>Anchor type used to locate the Infrastructure assembly (architecture tests, scanning).</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
