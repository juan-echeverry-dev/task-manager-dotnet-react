namespace TaskLane.Application.Identity.Ports;

/// <summary>
/// One-way, salted password hashing (ADR 0005). Passwords are never stored or returned in readable form.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}
