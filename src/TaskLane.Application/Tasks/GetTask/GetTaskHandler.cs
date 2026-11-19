using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.GetTask;

/// <summary>Returns one of the signed-in person's tasks (spec FR-010).</summary>
public sealed class GetTaskHandler(ITaskRepository tasks)
{
    public async Task<Result<TaskResponse>> HandleAsync(GetTaskQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var task = await tasks.GetOwnedAsync(query.TaskId, query.OwnerId, cancellationToken);

        return task is null
            ? Result.Failure<TaskResponse>(TaskErrors.NotFound)
            : Result.Success(TaskResponse.From(task));
    }
}
