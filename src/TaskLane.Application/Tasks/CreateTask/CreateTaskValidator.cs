using FluentValidation;

namespace TaskLane.Application.Tasks.CreateTask;

/// <summary>Create input rules (spec FR-003 to FR-006). Only the title is required.</summary>
public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(command => command.Title).TaskTitle();

        RuleFor(command => command.Description).TaskDescription();

        RuleFor(command => command.Status)
            .Must(status => status is null || TaskInputFormat.TryParseStatus(status, out _))
            .WithMessage(TaskFieldRules.StatusMessage);

        RuleFor(command => command.DueDate).TaskDueDate();
    }
}
