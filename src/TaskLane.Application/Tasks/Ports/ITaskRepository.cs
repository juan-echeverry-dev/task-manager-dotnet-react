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

    /// <summary>
    /// The owner's task, active or deleted, tracked for changes; <see langword="null"/> when it does not
    /// exist or belongs to someone else.
    /// </summary>
    Task<TaskItem?> GetOwnedAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);

    /// <summary>Saves the changes made to a task loaded with <see cref="GetOwnedAsync"/>.</summary>
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
}
