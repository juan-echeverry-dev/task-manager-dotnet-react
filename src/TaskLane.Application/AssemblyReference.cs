using System.Reflection;

namespace TaskLane.Application;

/// <summary>Anchor type used to locate the Application assembly (architecture tests, scanning).</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
