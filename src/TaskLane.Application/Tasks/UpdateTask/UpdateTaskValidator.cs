using FluentValidation;

namespace TaskLane.Application.Tasks.UpdateTask;

/// <summary>Edit input rules: the same as creation, except that the status is required.</summary>
public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(command => command.Title).TaskTitle();

        RuleFor(command => command.Description).TaskDescription();

        RuleFor(command => command.Status)
            .Cascade(CascadeMode.Stop)
            .Must(status => !string.IsNullOrWhiteSpace(status))
            .WithMessage("Status is required.")
            .Must(status => TaskInputFormat.TryParseStatus(status, out _))
            .WithMessage(TaskFieldRules.StatusMessage);

        RuleFor(command => command.DueDate).TaskDueDate();
    }
}
