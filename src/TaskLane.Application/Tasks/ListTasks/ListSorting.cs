using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>Sort fields as named in the query string, and each field's default direction (research R8).</summary>
internal static class ListSorting
{
    private static readonly Dictionary<string, TaskSortField> ActiveFields = new(StringComparer.Ordinal)
    {
        ["dueDate"] = TaskSortField.DueDate,
        ["createdAt"] = TaskSortField.CreatedAt,
        ["title"] = TaskSortField.Title,
    };

    private static readonly Dictionary<string, TaskSortField> DeletedFields = new(ActiveFields, StringComparer.Ordinal)
    {
        ["deletedAt"] = TaskSortField.DeletedAt,
    };

    public static string FieldNames(bool deleted) => string.Join(", ", Fields(deleted).Keys);

    public static bool TryParseField(string? value, bool deleted, out TaskSortField field) =>
        Fields(deleted).TryGetValue(value ?? string.Empty, out field);

    /// <summary>Most recently deleted first in the deleted list; earliest due date first otherwise.</summary>
    public static TaskSortField DefaultField(bool deleted) => deleted ? TaskSortField.DeletedAt : TaskSortField.DueDate;

    /// <summary>Dates of creation and deletion read newest first; due dates and titles read ascending.</summary>
    public static TaskSortDirection DefaultDirection(TaskSortField field) =>
        field is TaskSortField.CreatedAt or TaskSortField.DeletedAt ? TaskSortDirection.Descending : TaskSortDirection.Ascending;

    private static Dictionary<string, TaskSortField> Fields(bool deleted) => deleted ? DeletedFields : ActiveFields;
}
