namespace TaskLane.Application.Common.Results;

/// <summary>
/// Outcome of a use case that produces a value on success.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, Error? error)
        : base(error) => _value = value;

    /// <summary>The produced value. Throws when the result is a failure.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value; check IsSuccess first.");
}
