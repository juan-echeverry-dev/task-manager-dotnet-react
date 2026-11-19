using TaskLane.Application.Common.Results;

namespace TaskLane.Application.Tasks;

/// <summary>Expected failures of the Tasks use cases (contracts/tasks-api.md).</summary>
internal static class TaskErrors
{
    /// <summary>Same outcome for a missing task and another person's task (spec FR-002).</summary>
    public static Error NotFound { get; } = Error.NotFound("Task not found", "The task does not exist.");
}
