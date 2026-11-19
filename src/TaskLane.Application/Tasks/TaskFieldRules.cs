using System.Globalization;

using FluentValidation;

using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks;

/// <summary>
/// Field rules shared by the create and edit validators, so both report the same messages
/// (data-model.md). Sharing at two uses is a recorded deviation from the rule of three (plan.md).
/// </summary>
internal static class TaskFieldRules
{
    public static readonly string StatusMessage = $"Status must be one of: {TaskInputFormat.StatusNames}.";

    private static readonly string DescriptionMessage = string.Create(
        CultureInfo.InvariantCulture, $"Description must be at most {TaskItem.DescriptionMaxLength:N0} characters.");

    private static readonly string DueDateMessage = string.Create(
        CultureInfo.InvariantCulture,
        $"Enter a due date between {TaskItem.MinDueDate:yyyy-MM-dd} and {TaskItem.MaxDueDate:yyyy-MM-dd}.");

    /// <summary>Required; 1–200 characters after trimming.</summary>
    public static IRuleBuilderOptions<T, string> TaskTitle<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => title.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title must be at most {TaskItem.TitleMaxLength} characters.");

    /// <summary>Optional; at most 2,000 characters.</summary>
    public static IRuleBuilderOptions<T, string?> TaskDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(description => description is null || description.Length <= TaskItem.DescriptionMaxLength)
            .WithMessage(DescriptionMessage);

    /// <summary>Optional; a <c>yyyy-MM-dd</c> date between 2000-01-01 and 2099-12-31.</summary>
    public static IRuleBuilderOptions<T, string?> TaskDueDate<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(dueDate => dueDate is null || IsDueDateInRange(dueDate))
            .WithMessage(DueDateMessage);

    private static bool IsDueDateInRange(string value) =>
        TaskInputFormat.TryParseDate(value, out var date) && date >= TaskItem.MinDueDate && date <= TaskItem.MaxDueDate;
}
