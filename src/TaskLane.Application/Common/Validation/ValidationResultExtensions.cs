using FluentValidation.Results;

using TaskLane.Application.Common.Results;

namespace TaskLane.Application.Common.Validation;

internal static class ValidationResultExtensions
{
    /// <summary>Groups FluentValidation failures by field into a validation <see cref="Error"/>.</summary>
    public static Error ToError(this ValidationResult validation) =>
        Error.Validation(validation.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal));
}
