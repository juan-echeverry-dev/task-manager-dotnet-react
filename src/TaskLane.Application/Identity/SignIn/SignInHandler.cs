using FluentValidation;

using Microsoft.Extensions.Logging;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Common.Validation;
using TaskLane.Application.Identity.Ports;
using TaskLane.Domain.Identity;

namespace TaskLane.Application.Identity.SignIn;

/// <summary>
/// Verifies credentials and issues a 60-minute access token. Unknown email and wrong password are
/// indistinguishable in outcome, message, and work performed (spec FR-007).
/// </summary>
public sealed class SignInHandler(
    IValidator<SignInCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    ILogger<SignInHandler> logger)
{
    private const string TokenType = "Bearer";

    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Invalid credentials", "Invalid email or password.");

    public async Task<Result<SignInResponse>> HandleAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<SignInResponse>(validation.ToError());
        }

        var user = await users.GetByNormalizedEmailAsync(User.NormalizeEmail(command.Email), cancellationToken);

        // Always verify, even for an unknown email, so both failures cost the same.
        var passwordMatches = passwordHasher.Verify(user?.PasswordHash, command.Password);
        if (user is null || !passwordMatches)
        {
            IdentityLog.SignInFailed(logger);
            return Result.Failure<SignInResponse>(InvalidCredentials);
        }

        var token = tokenIssuer.Issue(user);
        IdentityLog.SignInSucceeded(logger, user.Id);

        return Result.Success(new SignInResponse(token.Value, TokenType, token.ExpiresAt));
    }
}
