namespace TaskLane.Application.Tasks.GetTask;

public sealed record GetTaskQuery(Guid OwnerId, Guid TaskId);
