namespace TaskLane.Api.Tasks;

/// <summary>Settings the task screens need, so they use the configured values (spec FR-023).</summary>
public sealed record TaskSettingsResponse(int DueDateRangeMaxDays);
