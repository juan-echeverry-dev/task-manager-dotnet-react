using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.RestoreTask;

/// <summary>
/// Returns a deleted task to the active list with all its values unchanged (spec FR-014). Restoring an
/// active task succeeds without changes.
/// </summary>
public sealed class RestoreTaskHandler(ITaskRepository tasks)
{
    public async Task<Result<TaskResponse>> HandleAsync(RestoreTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await tasks.GetOwnedAsync(command.TaskId, command.OwnerId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound);
        }

        task.Restore();
        await tasks.UpdateAsync(task, cancellationToken);

        return Result.Success(TaskResponse.From(task));
    }
}
