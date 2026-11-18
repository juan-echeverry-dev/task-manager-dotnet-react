namespace TaskLane.Application.Identity.SignIn;

public sealed record SignInResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
