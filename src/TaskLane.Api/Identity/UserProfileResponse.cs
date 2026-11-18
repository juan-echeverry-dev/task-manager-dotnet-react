namespace TaskLane.Api.Identity;

/// <summary>Public profile of an account. Never contains credential data (spec FR-011).</summary>
public sealed record UserProfileResponse(Guid Id, string DisplayName, string Email, DateTimeOffset CreatedAt);
