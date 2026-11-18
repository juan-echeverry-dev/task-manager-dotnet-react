using TaskLane.Domain.Identity;

namespace TaskLane.Application.Identity.Ports;

/// <summary>
/// Persistence port for user accounts (ADR 0004). Methods are intention-revealing, not generic CRUD.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Saves a new user. Returns <see langword="false"/> when another account already uses the same
    /// normalized email, including when that account was saved concurrently.
    /// </summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);
}
