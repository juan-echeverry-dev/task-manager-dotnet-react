namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>
/// Raw list input, as received in the query string. <paramref name="Deleted"/> selects the deleted-tasks list.
/// </summary>
public sealed record ListTasksQuery(
    Guid OwnerId,
    bool Deleted,
    string? Search,
    string? Status,
    string? DueFrom,
    string? DueTo,
    string? Sort,
    string? Direction,
    int? Page,
    int? PageSize);
