using System.Text.RegularExpressions;

namespace TaskLane.Application.Identity.Validation;

/// <summary>
/// Pragmatic email format check: one "@", no spaces, and a dot in the domain. Deliverability is not
/// verified (email verification is out of scope).
/// </summary>
internal static partial class EmailFormat
{
    public static bool IsValid(string email) => EmailPattern().IsMatch(email.Trim());

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex EmailPattern();
}
