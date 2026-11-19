namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>
/// Raw list input, as received in the query string.
/// </summary>
public sealed record ListTasksQuery(
    Guid OwnerId,
    string? Search,
    string? Status,
    string? DueFrom,
    string? DueTo,
    string? Sort,
    string? Direction,
    int? Page,
    int? PageSize);
