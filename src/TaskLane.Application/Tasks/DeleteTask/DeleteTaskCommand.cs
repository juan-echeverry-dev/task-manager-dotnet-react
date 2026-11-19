namespace TaskLane.Application.Tasks.DeleteTask;

public sealed record DeleteTaskCommand(Guid OwnerId, Guid TaskId);
