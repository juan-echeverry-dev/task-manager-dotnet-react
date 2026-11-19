using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks.Ports;

/// <summary>
/// A validated list request, already parsed and with defaults applied. Infrastructure translates it
/// into a query; Application stays free of persistence details.
/// </summary>
public sealed record TaskListQuery(
    bool Deleted,
    string? Search,
    TaskItemStatus? Status,
    DateOnly? DueFrom,
    DateOnly? DueTo,
    TaskSortField Sort,
    SortDirection Direction,
    int Page,
    int PageSize)
{
    public const int DefaultPageSize = 20;

    /// <summary>Active tasks by due date (earliest first), first page.</summary>
    public static TaskListQuery Default { get; } = new(
        Deleted: false,
        Search: null,
        Status: null,
        DueFrom: null,
        DueTo: null,
        TaskSortField.DueDate,
        SortDirection.Ascending,
        Page: 1,
        DefaultPageSize);
}

public enum TaskSortField
{
    DueDate,
    CreatedAt,
    Title,
    DeletedAt,
}

public enum SortDirection
{
    Ascending,
    Descending,
}
