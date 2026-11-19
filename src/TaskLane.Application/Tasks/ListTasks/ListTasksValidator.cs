using FluentValidation;

using Microsoft.Extensions.Options;

namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>List query rules (spec FR-016 to FR-018, FR-023; data-model.md).</summary>
public sealed class ListTasksValidator : AbstractValidator<ListTasksQuery>
{
    public const int MaxPageSize = 100;
    public const int SearchMaxLength = 200;

    public ListTasksValidator(IOptions<TaskListOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var maxRangeDays = options.Value.DueDateRangeMaxDays;

        RuleFor(query => query.Search)
            .Must(search => search is null || search.Trim().Length <= SearchMaxLength)
            .WithMessage($"Search must be at most {SearchMaxLength} characters.");

        RuleFor(query => query.Status)
            .Must(status => status is null || TaskInputFormat.TryParseStatus(status, out _))
            .WithMessage(TaskFieldRules.StatusMessage);

        RuleFor(query => query.DueFrom)
            .Must(BeADate).WithMessage(DueRangeMessages.Format)
            .Must((query, dueFrom) => dueFrom is not null || query.DueTo is null).WithMessage(DueRangeMessages.BothEnds);

        RuleFor(query => query.DueTo)
            .Cascade(CascadeMode.Stop)
            .Must(BeADate).WithMessage(DueRangeMessages.Format)
            .Must((query, dueTo) => dueTo is not null || query.DueFrom is null).WithMessage(DueRangeMessages.BothEnds)
            .Must((query, dueTo) => Span(query.DueFrom, dueTo) is not < 0).WithMessage(DueRangeMessages.Inverted)
            .Must((query, dueTo) => Span(query.DueFrom, dueTo) is not { } span || span <= maxRangeDays)
            .WithMessage($"Choose a range of at most {maxRangeDays} days.");

        RuleFor(query => query.Sort)
            .Must(sort => sort is null || ListSorting.TryParseField(sort, out _))
            .WithMessage($"Sort must be one of: {ListSorting.FieldNames}.");

        RuleFor(query => query.Direction)
            .Must(direction => direction is null || ListSorting.TryParseDirection(direction, out _))
            .WithMessage("Direction must be asc or desc.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .When(query => query.Page is not null)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .When(query => query.PageSize is not null)
            .WithMessage($"Page size must be between 1 and {MaxPageSize}.");
    }

    private static bool BeADate(string? value) => value is null || TaskInputFormat.TryParseDate(value, out _);

    /// <summary>Days from start to end; null unless both ends are valid dates.</summary>
    private static int? Span(string? dueFrom, string? dueTo) =>
        TaskInputFormat.TryParseDate(dueFrom, out var from) && TaskInputFormat.TryParseDate(dueTo, out var to)
            ? to.DayNumber - from.DayNumber
            : null;

    private static class DueRangeMessages
    {
        public const string Format = "Enter dates as YYYY-MM-DD.";
        public const string BothEnds = "Choose both a start and an end date.";
        public const string Inverted = "The start date must be on or before the end date.";
    }
}
