using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks;

/// <summary>
/// Read model of one task, shared by every Tasks use case. It never contains the owner, which is
/// implied by the session.
/// </summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TaskResponse From(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
