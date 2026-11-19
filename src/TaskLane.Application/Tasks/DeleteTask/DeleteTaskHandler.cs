using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.DeleteTask;

/// <summary>
/// Moves one of the signed-in person's tasks to the deleted state (spec FR-013). Deleting a deleted
/// task succeeds without changes.
/// </summary>
public sealed class DeleteTaskHandler(ITaskRepository tasks, TimeProvider timeProvider)
{
    public async Task<Result<TaskResponse>> HandleAsync(DeleteTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await tasks.GetOwnedAsync(command.TaskId, command.OwnerId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound);
        }

        task.Delete(timeProvider.GetUtcNow());
        await tasks.UpdateAsync(task, cancellationToken);

        return Result.Success(TaskResponse.From(task));
    }
}
