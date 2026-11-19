namespace TaskLane.Application.Tasks.CreateTask;

/// <summary>Raw create input. Status and due date arrive as text so the validator can report them.</summary>
public sealed record CreateTaskCommand(Guid OwnerId, string Title, string? Description, string? Status, string? DueDate);
