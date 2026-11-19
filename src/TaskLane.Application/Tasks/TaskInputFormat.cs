using System.Globalization;

using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks;

/// <summary>
/// Parses the text forms that tasks use in requests: status names and <c>yyyy-MM-dd</c> dates.
/// Validators use it to accept or reject input; handlers use it to convert input already validated.
/// </summary>
internal static class TaskInputFormat
{
    public const string StatusNames = "ToDo, InProgress, Done";

    /// <summary>Accepts the exact status names only (no numbers, no other casing).</summary>
    public static bool TryParseStatus(string? value, out TaskItemStatus status)
    {
        foreach (var candidate in Enum.GetValues<TaskItemStatus>())
        {
            if (string.Equals(value, candidate.ToString(), StringComparison.Ordinal))
            {
                status = candidate;
                return true;
            }
        }

        status = default;
        return false;
    }

    public static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static TaskItemStatus? ParseOptionalStatus(string? value) =>
        TryParseStatus(value, out var status) ? status : null;

    public static DateOnly? ParseOptionalDate(string? value) =>
        TryParseDate(value, out var date) ? date : null;
}
