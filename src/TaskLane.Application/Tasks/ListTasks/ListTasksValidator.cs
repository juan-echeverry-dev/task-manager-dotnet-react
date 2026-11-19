using FluentValidation;

namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>List query rules (spec FR-014, FR-016 to FR-018, FR-023).</summary>
public sealed class ListTasksValidator : AbstractValidator<ListTasksQuery>
{
    public const int MaxPageSize = 100;

    public ListTasksValidator()
    {
        RuleFor(query => query.Sort)
            .Must((query, sort) => ListSorting.TryParseField(sort, query.Deleted, out _))
            .When(query => query.Sort is not null)
            .WithMessage(query => $"Sort must be one of: {ListSorting.FieldNames(query.Deleted)}.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .When(query => query.Page is not null)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .When(query => query.PageSize is not null)
            .WithMessage($"Page size must be between 1 and {MaxPageSize}.");
    }
}
