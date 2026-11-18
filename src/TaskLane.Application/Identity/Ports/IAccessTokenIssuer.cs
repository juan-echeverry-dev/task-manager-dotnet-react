using TaskLane.Domain.Identity;

namespace TaskLane.Application.Identity.Ports;

/// <summary>Issues the signed session credential returned by a successful sign-in (ADR 0006).</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
