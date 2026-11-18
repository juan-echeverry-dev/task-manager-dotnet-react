namespace TaskLane.Application.Identity.GetProfile;

public sealed record ProfileResponse(Guid Id, string DisplayName, string Email, DateTimeOffset CreatedAt);
