using TaskLane.Application.Tasks.CreateTask;

namespace TaskLane.UnitTests.Tasks.CreateTask;

public sealed class CreateTaskValidatorTests
{
    private readonly CreateTaskValidator _validator = new();

    private static CreateTaskCommand Valid() =>
        new(Guid.CreateVersion7(), "Prepare demo", "Slides", "InProgress", "2026-12-01");

    [Fact]
    public void ValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void OnlyTitle_Passes()
    {
        _validator.Validate(new CreateTaskCommand(Guid.CreateVersion7(), "Prepare demo", null, null, null)).IsValid.ShouldBeTrue();
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
    public void Title_Of200PlusSurroundingSpaces_Passes()
    {
        ErrorsFor(Valid() with { Title = $"  {new string('t', 200)}  " }, "Title").ShouldBeEmpty();
    }

    [Fact]
    public void Description_LongerThan2000_IsRejected()
    {
        ErrorsFor(Valid() with { Description = new string('d', 2001) }, "Description")
            .ShouldBe(["Description must be at most 2,000 characters."]);
    }

    [Fact]
    public void Description_Of2000_Passes()
    {
        ErrorsFor(Valid() with { Description = new string('d', 2000) }, "Description").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ToDo")]
    [InlineData("InProgress")]
    [InlineData("Done")]
    [InlineData(null)]
    public void Status_KnownOrMissing_Passes(string? status)
    {
        ErrorsFor(Valid() with { Status = status }, "Status").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("todo")]
    [InlineData("Finished")]
    [InlineData("0")]
    [InlineData("")]
    public void Status_Unknown_IsRejected(string status)
    {
        ErrorsFor(Valid() with { Status = status }, "Status")
            .ShouldBe(["Status must be one of: ToDo, InProgress, Done."]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2000-01-01")]
    [InlineData("2099-12-31")]
    [InlineData("2020-02-29")]
    public void DueDate_MissingOrInRange_Passes(string? dueDate)
    {
        ErrorsFor(Valid() with { DueDate = dueDate }, "DueDate").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("1999-12-31")]
    [InlineData("2100-01-01")]
    [InlineData("2026-02-30")]
    [InlineData("01/12/2026")]
    [InlineData("2026-12-1")]
    [InlineData("tomorrow")]
    [InlineData("")]
    public void DueDate_InvalidOrOutOfRange_IsRejected(string dueDate)
    {
        ErrorsFor(Valid() with { DueDate = dueDate }, "DueDate")
            .ShouldBe(["Enter a due date between 2000-01-01 and 2099-12-31."]);
    }

    [Fact]
    public void AllInvalidFields_AreReportedTogether()
    {
        var result = _validator.Validate(
            new CreateTaskCommand(Guid.CreateVersion7(), " ", new string('d', 2001), "Later", "2100-01-01"));

        result.Errors.Select(error => error.PropertyName).Distinct()
            .ShouldBe(["Title", "Description", "Status", "DueDate"], ignoreOrder: true);
    }

    private string[] ErrorsFor(CreateTaskCommand command, string propertyName) =>
        [.. _validator.Validate(command).Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)];
}
