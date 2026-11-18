using System.Reflection;

namespace TaskLane.ArchitectureTests;

internal static class Layers
{
    public static readonly Assembly Domain = TaskLane.Domain.AssemblyReference.Assembly;
    public static readonly Assembly Application = TaskLane.Application.AssemblyReference.Assembly;
    public static readonly Assembly Infrastructure = TaskLane.Infrastructure.AssemblyReference.Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}
