using TaskLane.Application.Tasks.UpdateTask;

namespace TaskLane.UnitTests.Tasks.UpdateTask;

public sealed class UpdateTaskValidatorTests
{
    private readonly UpdateTaskValidator _validator = new();

    private static UpdateTaskCommand Valid() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "Prepare demo", "Slides", "Done", "2026-12-01");

    [Fact]
    public void ValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void WithoutDescriptionAndDueDate_Passes()
    {
        _validator.Validate(Valid() with { Description = null, DueDate = null }).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_Blank_IsRejected(string title)
    {
        ErrorsFor(Valid() with { Title = title }, "Title").ShouldBe(["Title is required."]);
    }

    [Fact]
    public void Title_LongerThan200AfterTrim_IsRejected()
    {
        ErrorsFor(Valid() with { Title = new string('t', 201) }, "Title")
            .ShouldBe(["Title must be at most 200 characters."]);
    }

    [Fact]
    public void Description_LongerThan2000_IsRejected()
    {
        ErrorsFor(Valid() with { Description = new string('d', 2001) }, "Description")
            .ShouldBe(["Description must be at most 2,000 characters."]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Status_Missing_IsRejected(string? status)
    {
        ErrorsFor(Valid() with { Status = status }, "Status").ShouldBe(["Status is required."]);
    }

    [Theory]
    [InlineData("done")]
    [InlineData("Finished")]
    [InlineData("2")]
    public void Status_Unknown_IsRejected(string status)
    {
        ErrorsFor(Valid() with { Status = status }, "Status")
            .ShouldBe(["Status must be one of: ToDo, InProgress, Done."]);
    }

    [Theory]
    [InlineData("1999-12-31")]
    [InlineData("2100-01-01")]
    [InlineData("2026-13-01")]
    [InlineData("")]
    public void DueDate_InvalidOrOutOfRange_IsRejected(string dueDate)
    {
        ErrorsFor(Valid() with { DueDate = dueDate }, "DueDate")
            .ShouldBe(["Enter a due date between 2000-01-01 and 2099-12-31."]);
    }

    [Fact]
    public void AllInvalidFields_AreReportedTogether()
    {
        var result = _validator.Validate(Valid() with
        {
            Title = "",
            Description = new string('d', 2001),
            Status = null,
            DueDate = "nope",
        });

        result.Errors.Select(error => error.PropertyName).Distinct()
            .ShouldBe(["Title", "Description", "Status", "DueDate"], ignoreOrder: true);
    }

    private string[] ErrorsFor(UpdateTaskCommand command, string propertyName) =>
        [.. _validator.Validate(command).Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)];
}
