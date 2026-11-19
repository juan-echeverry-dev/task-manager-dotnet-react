namespace TaskLane.Api.Tasks;

/// <summary>
/// Query string of both task lists (contracts/tasks-api.md). Values arrive as text so validation errors
/// come from Application, keyed by these parameter names.
/// </summary>
public sealed class TaskListRequest
{
    public string? Search { get; init; }

    public string? Status { get; init; }

    public string? DueFrom { get; init; }

    public string? DueTo { get; init; }

    public string? Sort { get; init; }

    public string? Direction { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
