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
}
