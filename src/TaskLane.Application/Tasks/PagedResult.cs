namespace TaskLane.Application.Tasks;

/// <summary>One page of a list and the total number of matching items across all pages.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
