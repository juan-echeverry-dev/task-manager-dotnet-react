using Microsoft.AspNetCore.Mvc;

using TaskLane.Api.Common;
using TaskLane.Application.Identity.GetProfile;

namespace TaskLane.Api.Identity;

/// <summary>
/// Endpoints for the signed-in person. Authentication is required by the fallback policy.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetMe([FromServices] GetProfileHandler handler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // The token is validated by then; a subject that is not a user id is still not a session.
        if (User.GetUserId() is not { } userId)
        {
            return Problem(
                title: "Authentication required",
                detail: "Your session is no longer valid. Please sign in again.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetProfileQuery(userId), cancellationToken);

        return result.ToActionResult(profile => Ok(
            new UserProfileResponse(profile.Id, profile.DisplayName, profile.Email, profile.CreatedAt)));
    }
}
