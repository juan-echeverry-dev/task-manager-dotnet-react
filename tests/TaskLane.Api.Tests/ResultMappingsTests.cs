using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using TaskLane.Api.Common;
using TaskLane.Application.Common.Results;

namespace TaskLane.Api.Tests;

public sealed class ResultMappingsTests
{
    [Fact]
    public void Success_UsesTheSuccessMapping()
    {
        var expected = new OkObjectResult("value");

        var actionResult = Result.Success("value").ToActionResult(_ => expected);

        actionResult.ShouldBeSameAs(expected);
    }

    [Fact]
    public void ValidationError_MapsTo400WithCamelCaseFieldErrors()
    {
        var error = Error.Validation(new Dictionary<string, string[]>
        {
            ["DisplayName"] = ["Display name is required."],
            ["Email"] = ["Enter a valid email address."],
        });

        var objectResult = Result.Failure<string>(error).ToActionResult(_ => new OkResult()).ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.ShouldContain("application/problem+json");
        var problem = objectResult.Value.ShouldBeOfType<ValidationProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Errors.Keys.ShouldBe(["displayName", "email"], ignoreOrder: true);
        problem.Errors["email"].ShouldBe(["Enter a valid email address."]);
    }

    [Theory]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    public void OtherErrors_MapToStatusWithTitleAndDetail(ErrorType type, int expectedStatus)
    {
        var error = type switch
        {
            ErrorType.Conflict => Error.Conflict("Title", "Detail"),
            ErrorType.Unauthorized => Error.Unauthorized("Title", "Detail"),
            _ => Error.NotFound("Title", "Detail"),
        };

        var objectResult = Result.Failure<string>(error).ToActionResult(_ => new OkResult()).ShouldBeOfType<ObjectResult>();

        objectResult.StatusCode.ShouldBe(expectedStatus);
        objectResult.ContentTypes.ShouldContain("application/problem+json");
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(expectedStatus);
        problem.Title.ShouldBe("Title");
        problem.Detail.ShouldBe("Detail");
    }
}
