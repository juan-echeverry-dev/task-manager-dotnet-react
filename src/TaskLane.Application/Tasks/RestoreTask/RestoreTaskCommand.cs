namespace TaskLane.Application.Tasks.RestoreTask;

public sealed record RestoreTaskCommand(Guid OwnerId, Guid TaskId);
