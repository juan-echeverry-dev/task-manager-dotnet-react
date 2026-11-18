using TaskLane.Application.Common.Results;
using TaskLane.Application.Identity.Ports;

namespace TaskLane.Application.Identity.GetProfile;

public sealed class GetProfileHandler(IUserRepository users)
{
    public async Task<Result<ProfileResponse>> HandleAsync(GetProfileQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var user = await users.GetByIdAsync(query.UserId, cancellationToken);

        // A valid session for an account that no longer exists is treated as no session (spec Q3).
        return user is null
            ? Result.Failure<ProfileResponse>(
                Error.Unauthorized("Authentication required", "Your session is no longer valid. Please sign in again."))
            : Result.Success(new ProfileResponse(user.Id, user.DisplayName, user.Email, user.CreatedAt));
    }
}
