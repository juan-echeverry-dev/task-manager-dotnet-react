using FluentValidation;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Common.Validation;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks.CreateTask;

/// <summary>Creates a task owned by the signed-in person (spec FR-001, FR-009).</summary>
public sealed class CreateTaskHandler(
    IValidator<CreateTaskCommand> validator,
    ITaskRepository tasks,
    TimeProvider timeProvider)
{
    public async Task<Result<TaskResponse>> HandleAsync(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<TaskResponse>(validation.ToError());
        }

        var now = timeProvider.GetUtcNow();
        var task = TaskItem.Create(
            Guid.CreateVersion7(now),
            command.OwnerId,
            command.Title,
            command.Description,
            TaskInputFormat.ParseOptionalStatus(command.Status),
            TaskInputFormat.ParseOptionalDate(command.DueDate),
            now);

        await tasks.AddAsync(task, cancellationToken);

        return Result.Success(TaskResponse.From(task));
    }
}
