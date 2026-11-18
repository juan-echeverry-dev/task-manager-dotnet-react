namespace TaskLane.Domain.Identity;

/// <summary>
/// A person who can sign in. Created only through <see cref="Register"/>, which enforces the invariants.
/// </summary>
public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;

    // Required by EF Core to materialize entities.
    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Email as entered, without surrounding spaces.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Lower-cased email used for uniqueness and sign-in lookups.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static User Register(Guid id, string email, string displayName, string passwordHash, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The user id must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var trimmedEmail = email.Trim();
        var trimmedDisplayName = displayName.Trim();

        if (trimmedEmail.Length > EmailMaxLength)
        {
            throw new ArgumentException($"The email must be at most {EmailMaxLength} characters.", nameof(email));
        }

        if (trimmedDisplayName.Length > DisplayNameMaxLength)
        {
            throw new ArgumentException(
                $"The display name must be at most {DisplayNameMaxLength} characters.", nameof(displayName));
        }

        if (createdAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The creation time must be expressed in UTC.", nameof(createdAt));
        }

        return new User
        {
            Id = id,
            Email = trimmedEmail,
            NormalizedEmail = NormalizeEmail(trimmedEmail),
            DisplayName = trimmedDisplayName,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
        };
    }

    /// <summary>The single normalization rule shared by registration and sign-in.</summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Trim().ToLowerInvariant();
    }
}
