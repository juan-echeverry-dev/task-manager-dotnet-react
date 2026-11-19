namespace TaskLane.Api.Tasks;

/// <summary>
/// Body of <c>PUT /api/tasks/{id}</c>: a full replacement. A null or missing description or due date
/// clears it. Fields are nullable so validation errors come from Application.
/// </summary>
public sealed record UpdateTaskRequest(string? Title, string? Description, string? Status, string? DueDate);
