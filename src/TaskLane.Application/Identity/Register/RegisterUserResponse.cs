namespace TaskLane.Application.Identity.Register;

public sealed record RegisterUserResponse(Guid Id, string DisplayName, string Email, DateTimeOffset CreatedAt);
