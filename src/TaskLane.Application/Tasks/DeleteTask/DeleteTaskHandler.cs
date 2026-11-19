using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.DeleteTask;

/// <summary>
/// Permanently removes one of the signed-in person's tasks (spec 003 FR-001 to FR-003). A missing,
/// foreign, or already deleted task is "not found".
/// </summary>
public sealed class DeleteTaskHandler(ITaskRepository tasks)
{
    public async Task<Result<Guid>> HandleAsync(DeleteTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await tasks.DeleteOwnedAsync(command.TaskId, command.OwnerId, cancellationToken)
            ? Result.Success(command.TaskId)
            : Result.Failure<Guid>(TaskErrors.NotFound);
    }
}
