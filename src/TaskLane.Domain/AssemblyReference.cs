using System.Reflection;

namespace TaskLane.Domain;

/// <summary>Anchor type used to locate the Domain assembly (architecture tests, scanning).</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
