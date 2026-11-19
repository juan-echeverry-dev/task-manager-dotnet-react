using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>Sort fields as named in the query string, and each field's default direction (research R8).</summary>
internal static class ListSorting
{
    private static readonly Dictionary<string, TaskSortField> Fields = new(StringComparer.Ordinal)
    {
        ["dueDate"] = TaskSortField.DueDate,
        ["createdAt"] = TaskSortField.CreatedAt,
        ["title"] = TaskSortField.Title,
    };

    public static string FieldNames { get; } = string.Join(", ", Fields.Keys);

    public static bool TryParseField(string? value, out TaskSortField field) =>
        Fields.TryGetValue(value ?? string.Empty, out field);

    /// <summary>Accepts <c>asc</c> and <c>desc</c> exactly.</summary>
    public static bool TryParseDirection(string? value, out TaskSortDirection direction)
    {
        (var known, direction) = value switch
        {
            "asc" => (true, TaskSortDirection.Ascending),
            "desc" => (true, TaskSortDirection.Descending),
            _ => (false, default),
        };
        return known;
    }

    /// <summary>Creation dates read newest first; due dates and titles read ascending.</summary>
    public static TaskSortDirection DefaultDirection(TaskSortField field) =>
        field is TaskSortField.CreatedAt ? TaskSortDirection.Descending : TaskSortDirection.Ascending;
}
