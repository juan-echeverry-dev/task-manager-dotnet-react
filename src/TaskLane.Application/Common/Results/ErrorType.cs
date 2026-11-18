namespace TaskLane.Application.Common.Results;

/// <summary>
/// Category of an expected failure. The API maps each category to an HTTP status code.
/// </summary>
public enum ErrorType
{
    Validation,
    Conflict,
    Unauthorized,
    NotFound,
}
