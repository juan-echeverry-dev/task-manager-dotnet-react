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
    /// The owner's task, tracked for changes; <see langword="null"/> when it does not exist or belongs to
    /// someone else.
    /// </summary>
    Task<TaskItem?> GetOwnedAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the changes made to a task loaded with <see cref="GetOwnedAsync"/>; <see langword="false"/>
    /// when the task no longer exists (deleted after it was loaded).
    /// </summary>
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently removes the owner's task; <see langword="false"/> when no row of this owner matched.
    /// </summary>
    Task<bool> DeleteOwnedAsync(Guid id, Guid ownerId, CancellationToken cancellationToken);
}
