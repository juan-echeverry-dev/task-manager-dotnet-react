using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskLane.Api.Common;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in person's id from the <c>sub</c> claim, or <see langword="null"/> when the claim is
    /// missing or is not a user id. This is the only contract other modules use to know who is signed in.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            ? userId
            : null;
    }
}
