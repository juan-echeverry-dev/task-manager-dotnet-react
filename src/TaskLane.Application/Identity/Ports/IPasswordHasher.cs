namespace TaskLane.Application.Identity.Ports;

/// <summary>
/// One-way, salted password hashing (ADR 0005). Passwords are never stored or returned in readable form.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Checks a password against a stored hash. When <paramref name="passwordHash"/> is
    /// <see langword="null"/> (unknown account), performs equivalent work and returns
    /// <see langword="false"/>, so response time does not reveal whether an account exists.
    /// </summary>
    bool Verify(string? passwordHash, string password);
}
