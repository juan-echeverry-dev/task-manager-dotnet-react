using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks.Ports;

/// <summary>
/// Persistence port for tasks. Every read is scoped to the owner inside the query itself, so another
/// person's task is indistinguishable from a missing one.
/// </summary>
public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<PagedResult<TaskItem>> ListAsync(Guid ownerId, TaskListQuery query, CancellationToken cancellationToken);
}
