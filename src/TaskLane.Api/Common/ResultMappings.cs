using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using TaskLane.Application.Common.Results;

namespace TaskLane.Api.Common;

/// <summary>
/// Translates use-case results into HTTP responses; failures become RFC 9457 Problem Details (ADR 0010).
/// </summary>
internal static class ResultMappings
{
    private const string ProblemJson = "application/problem+json";

    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : ToProblemResult(result.Error!);
    }

    private static ObjectResult ToProblemResult(Error error)
    {
        var problem = error.Type == ErrorType.Validation
            ? new ValidationProblemDetails(ToCamelCase(error.ValidationErrors))
            : new ProblemDetails();

        problem.Status = StatusCodeFor(error.Type);
        problem.Title = error.Title;
        problem.Detail = error.Detail;

        var objectResult = new ObjectResult(problem) { StatusCode = problem.Status };
        objectResult.ContentTypes.Add(ProblemJson);

        return objectResult;
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
    };

    // Field names follow the JSON request bodies (camelCase), so the SPA can match them to inputs.
    private static Dictionary<string, string[]> ToCamelCase(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key), pair => pair.Value);
}
