using System.Diagnostics.CodeAnalysis;

namespace TaskLane.Application.Common.Results;

/// <summary>
/// An expected failure of a use case, returned as a value instead of thrown (ADR 0007).
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "C#-only code base; 'Error' is the conventional name in the Result pattern.")]
public sealed record Error
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    private Error(ErrorType type, string title, string detail, IReadOnlyDictionary<string, string[]> validationErrors)
    {
        Type = type;
        Title = title;
        Detail = detail;
        ValidationErrors = validationErrors;
    }

    public ErrorType Type { get; }

    public string Title { get; }

    public string Detail { get; }

    /// <summary>Messages per input field; empty unless <see cref="Type"/> is <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(ErrorType.Validation, "Validation failed", "One or more fields are invalid.", validationErrors);

    public static Error Conflict(string title, string detail) => new(ErrorType.Conflict, title, detail, NoValidationErrors);

    public static Error Unauthorized(string title, string detail) => new(ErrorType.Unauthorized, title, detail, NoValidationErrors);

    public static Error NotFound(string title, string detail) => new(ErrorType.NotFound, title, detail, NoValidationErrors);
}
