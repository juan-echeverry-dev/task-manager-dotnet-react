namespace TaskLane.Application.Identity.Ports;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
