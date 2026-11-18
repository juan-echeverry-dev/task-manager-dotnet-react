using FluentValidation;

using Microsoft.Extensions.Logging;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Common.Validation;
using TaskLane.Application.Identity.Ports;
using TaskLane.Domain.Identity;

namespace TaskLane.Application.Identity.Register;

/// <summary>
/// Creates an account. Does not start a session (spec FR-005).
/// </summary>
public sealed class RegisterUserHandler(
    IValidator<RegisterUserCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<RegisterUserHandler> logger)
{
    public async Task<Result<RegisterUserResponse>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<RegisterUserResponse>(validation.ToError());
        }

        var now = timeProvider.GetUtcNow();
        var user = User.Register(
            Guid.CreateVersion7(now),
            command.Email,
            command.DisplayName,
            passwordHasher.Hash(command.Password),
            now);

        // No "exists?" pre-check: the unique index decides, so concurrent registrations cannot both win.
        if (!await users.TryAddAsync(user, cancellationToken))
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Conflict("Email already registered", "An account with this email already exists."));
        }

        IdentityLog.UserRegistered(logger, user.Id);

        return Result.Success(new RegisterUserResponse(user.Id, user.DisplayName, user.Email, user.CreatedAt));
    }
}
