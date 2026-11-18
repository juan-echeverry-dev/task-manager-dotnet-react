namespace TaskLane.Application.Common.Results;

/// <summary>
/// Outcome of a use case: either success or an expected <see cref="Results.Error"/> (ADR 0007).
/// </summary>
public abstract class Result
{
    private protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Failure<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(default, error);
    }
}
