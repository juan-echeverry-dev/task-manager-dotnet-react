namespace TaskLane.Infrastructure.Identity;

/// <summary>
/// Settings for issuing and validating access tokens (ADR 0006), bound from <see cref="SectionName"/>.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Base64-encoded symmetric key. Never committed except the marked development-only key.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;

    public byte[] GetSigningKeyBytes() => Convert.FromBase64String(SigningKey);

    internal bool HasValidSigningKey()
    {
        // Configuration binding assigns null when the key is present but empty.
        if (string.IsNullOrEmpty(SigningKey))
        {
            return false;
        }

        var buffer = new byte[SigningKey.Length];

        return Convert.TryFromBase64String(SigningKey, buffer, out var bytesWritten)
            && bytesWritten >= MinimumSigningKeyBytes;
    }
}
