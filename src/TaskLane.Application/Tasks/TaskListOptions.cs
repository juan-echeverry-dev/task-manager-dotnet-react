namespace TaskLane.Application.Tasks;

/// <summary>
/// List settings that whoever runs the application can change without a new release (spec FR-023),
/// bound from the <c>Tasks</c> configuration section (for example <c>Tasks__DueDateRangeMaxDays=30</c>).
/// </summary>
public sealed class TaskListOptions
{
    public const string SectionName = "Tasks";
    public const int MinDueDateRangeMaxDays = 1;
    public const int MaxDueDateRangeMaxDays = 3650;

    /// <summary>Longest span, in days, allowed for the due-date range filter.</summary>
    public int DueDateRangeMaxDays { get; set; } = 90;

    public bool IsValid() => DueDateRangeMaxDays is >= MinDueDateRangeMaxDays and <= MaxDueDateRangeMaxDays;
}
