using FluentValidation;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Common.Validation;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.UpdateTask;

/// <summary>
/// Replaces the editable values of one of the signed-in person's tasks (spec FR-011).
/// The last save wins.
/// </summary>
public sealed class UpdateTaskHandler(
    IValidator<UpdateTaskCommand> validator,
    ITaskRepository tasks,
    TimeProvider timeProvider)
{
    public async Task<Result<TaskResponse>> HandleAsync(UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<TaskResponse>(validation.ToError());
        }

        var task = await tasks.GetOwnedAsync(command.TaskId, command.OwnerId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound);
        }

        // The validator guarantees a known status here.
        task.Update(
            command.Title,
            command.Description,
            TaskInputFormat.ParseOptionalStatus(command.Status)!.Value,
            TaskInputFormat.ParseOptionalDate(command.DueDate),
            timeProvider.GetUtcNow());

        // The task may have been deleted between loading and saving.
        if (!await tasks.UpdateAsync(task, cancellationToken))
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound);
        }

        return Result.Success(TaskResponse.From(task));
    }
}
