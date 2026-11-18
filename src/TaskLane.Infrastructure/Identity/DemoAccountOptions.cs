namespace TaskLane.Infrastructure.Identity;

/// <summary>
/// Demo account provisioned in local development only (spec FR-016), bound from <see cref="SectionName"/>.
/// </summary>
public sealed class DemoAccountOptions
{
    public const string SectionName = "DemoAccount";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    internal bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password) && !string.IsNullOrWhiteSpace(DisplayName);
}
