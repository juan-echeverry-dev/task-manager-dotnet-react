namespace TaskLane.Api.Tasks;

/// <summary>Body of <c>POST /api/tasks</c>. Fields are nullable so validation errors come from Application.</summary>
public sealed record CreateTaskRequest(string? Title, string? Description, string? Status, string? DueDate);
