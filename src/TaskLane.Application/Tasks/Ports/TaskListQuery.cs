using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks.Ports;

/// <summary>
/// A validated list request, already parsed and with defaults applied. Infrastructure translates it
/// into a query; Application stays free of persistence details.
/// </summary>
public sealed record TaskListQuery(
    string? Search,
    TaskItemStatus? Status,
    DateOnly? DueFrom,
    DateOnly? DueTo,
    TaskSortField Sort,
    TaskSortDirection Direction,
    int Page,
    int PageSize)
{
    public const int DefaultPageSize = 20;

    /// <summary>Tasks by due date (earliest first), first page.</summary>
    public static TaskListQuery Default { get; } = new(
        Search: null,
        Status: null,
        DueFrom: null,
        DueTo: null,
        TaskSortField.DueDate,
        TaskSortDirection.Ascending,
        Page: 1,
        DefaultPageSize);
}

public enum TaskSortField
{
    DueDate,
    CreatedAt,
    Title,
}

public enum TaskSortDirection
{
    Ascending,
    Descending,
}
