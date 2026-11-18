using Microsoft.AspNetCore.Identity;

using TaskLane.Domain.Identity;

namespace TaskLane.Infrastructure.Identity;

/// <summary>
/// Adapts the framework's <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA512, per-hash salt) to
/// the Application port (ADR 0005).
/// </summary>
internal sealed class PasswordHasherAdapter : Application.Identity.Ports.IPasswordHasher
{
    // The framework hasher ignores the user argument; a single instance keeps its options.
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
