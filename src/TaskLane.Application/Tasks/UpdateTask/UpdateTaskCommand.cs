namespace TaskLane.Application.Tasks.UpdateTask;

/// <summary>Full replacement of a task's editable values; a null description or due date clears it.</summary>
public sealed record UpdateTaskCommand(
    Guid OwnerId,
    Guid TaskId,
    string Title,
    string? Description,
    string? Status,
    string? DueDate);
