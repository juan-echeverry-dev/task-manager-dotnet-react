using System.Globalization;

using FluentValidation;

using TaskLane.Domain.Tasks;

namespace TaskLane.Application.Tasks.CreateTask;

/// <summary>Create input rules (spec FR-003 to FR-006). Lengths are measured after trimming the title.</summary>
public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => title.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"Title must be at most {TaskItem.TitleMaxLength} characters.");

        RuleFor(command => command.Description)
            .Must(description => description is null || description.Length <= TaskItem.DescriptionMaxLength)
            .WithMessage(string.Create(
                CultureInfo.InvariantCulture, $"Description must be at most {TaskItem.DescriptionMaxLength:N0} characters."));

        RuleFor(command => command.Status)
            .Must(status => TaskInputFormat.TryParseStatus(status, out _))
            .When(command => command.Status is not null)
            .WithMessage($"Status must be one of: {TaskInputFormat.StatusNames}.");

        RuleFor(command => command.DueDate)
            .Must(dueDate => TaskInputFormat.TryParseDate(dueDate, out var date)
                && date >= TaskItem.MinDueDate
                && date <= TaskItem.MaxDueDate)
            .When(command => command.DueDate is not null)
            .WithMessage(string.Create(
                CultureInfo.InvariantCulture,
                $"Enter a due date between {TaskItem.MinDueDate:yyyy-MM-dd} and {TaskItem.MaxDueDate:yyyy-MM-dd}."));
    }
}
