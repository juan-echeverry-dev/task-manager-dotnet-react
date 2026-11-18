using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TaskLane.Api.Common;
using TaskLane.Application.Identity.Register;

namespace TaskLane.Api.Identity;

/// <summary>
/// Anonymous account endpoints: registration and sign-in (contracts/identity-api.md).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string ProfileLocation = "/api/users/me";

    [HttpPost("register")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        [FromServices] RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.HandleAsync(
            new RegisterUserCommand(request.DisplayName ?? string.Empty, request.Email ?? string.Empty, request.Password ?? string.Empty),
            cancellationToken);

        return result.ToActionResult(user => Created(
            ProfileLocation,
            new UserProfileResponse(user.Id, user.DisplayName, user.Email, user.CreatedAt)));
    }
}
