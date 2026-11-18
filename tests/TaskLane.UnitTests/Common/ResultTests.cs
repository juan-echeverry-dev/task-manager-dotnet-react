using TaskLane.Application.Common.Results;

namespace TaskLane.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_ExposesError()
    {
        var error = Error.Conflict("Title", "Detail");

        var result = Result.Failure<int>(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        var result = Result.Failure<int>(Error.NotFound("Title", "Detail"));

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_WithoutError_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void Validation_CarriesFieldErrors()
    {
        var fieldErrors = new Dictionary<string, string[]> { ["Email"] = ["Email is required."] };

        var error = Error.Validation(fieldErrors);

        error.Type.ShouldBe(ErrorType.Validation);
        error.ValidationErrors.ShouldBe(fieldErrors);
    }

    [Theory]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.NotFound)]
    public void Factories_SetTypeTitleAndDetail(ErrorType type)
    {
        var error = type switch
        {
            ErrorType.Conflict => Error.Conflict("Title", "Detail"),
            ErrorType.Unauthorized => Error.Unauthorized("Title", "Detail"),
            _ => Error.NotFound("Title", "Detail"),
        };

        error.Type.ShouldBe(type);
        error.Title.ShouldBe("Title");
        error.Detail.ShouldBe("Detail");
        error.ValidationErrors.ShouldBeEmpty();
    }
}
