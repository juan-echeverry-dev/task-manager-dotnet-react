using Microsoft.Extensions.Logging;

namespace TaskLane.Application.Identity;

/// <summary>
/// Authentication events (spec FR-019). Only account identifiers are logged: never passwords,
/// session credentials, or email addresses.
/// </summary>
internal static partial class IdentityLog
{
    [LoggerMessage(EventId = 1000, EventName = "UserRegistered", Level = LogLevel.Information,
        Message = "User {UserId} registered")]
    public static partial void UserRegistered(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1001, EventName = "SignInSucceeded", Level = LogLevel.Information,
        Message = "User {UserId} signed in")]
    public static partial void SignInSucceeded(ILogger logger, Guid userId);

    // Deliberately carries no data: the entered email may be personal data or a mistyped password.
    [LoggerMessage(EventId = 1002, EventName = "SignInFailed", Level = LogLevel.Warning,
        Message = "Sign-in failed: invalid email or password")]
    public static partial void SignInFailed(ILogger logger);
}
