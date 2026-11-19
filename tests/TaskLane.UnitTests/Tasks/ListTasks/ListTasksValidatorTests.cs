using Microsoft.Extensions.Options;

using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.ListTasks;

namespace TaskLane.UnitTests.Tasks.ListTasks;

public sealed class ListTasksValidatorTests
{
    private readonly ListTasksValidator _validator = new(Options.Create(new TaskListOptions()));

    private static ListTasksQuery Active() =>
        new(Guid.CreateVersion7(), null, null, null, null, null, null, null, null);

    [Fact]
    public void NoParameters_Passes()
    {
        _validator.Validate(Active()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    public void Page_OneOrGreater_Passes(int page)
    {
        ErrorsFor(Active() with { Page = page }, "Page").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_BelowOne_IsRejected(int page)
    {
        ErrorsFor(Active() with { Page = page }, "Page").ShouldBe(["Page must be 1 or greater."]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void PageSize_Within1And100_Passes(int pageSize)
    {
        ErrorsFor(Active() with { PageSize = pageSize }, "PageSize").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_OutOfRange_IsRejected(int pageSize)
    {
        ErrorsFor(Active() with { PageSize = pageSize }, "PageSize")
            .ShouldBe(["Page size must be between 1 and 100."]);
    }

    [Theory]
    [InlineData("dueDate")]
    [InlineData("createdAt")]
    [InlineData("title")]
    public void Sort_KnownField_Passes(string sort)
    {
        ErrorsFor(Active() with { Sort = sort }, "Sort").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("DueDate")]
    [InlineData("priority")]
    [InlineData("deletedAt")]
    [InlineData("")]
    public void Sort_Unknown_IsRejectedWithTheKnownFields(string sort)
    {
        ErrorsFor(Active() with { Sort = sort }, "Sort")
            .ShouldBe(["Sort must be one of: dueDate, createdAt, title."]);
    }

    [Fact]
    public void Search_Of200AfterTrim_Passes()
    {
        ErrorsFor(Active() with { Search = $"  {new string('s', 200)}  " }, "Search").ShouldBeEmpty();
    }

    [Fact]
    public void Search_LongerThan200AfterTrim_IsRejected()
    {
        ErrorsFor(Active() with { Search = new string('s', 201) }, "Search")
            .ShouldBe(["Search must be at most 200 characters."]);
    }

    [Theory]
    [InlineData("ToDo")]
    [InlineData("InProgress")]
    [InlineData("Done")]
    public void Status_Known_Passes(string status)
    {
        ErrorsFor(Active() with { Status = status }, "Status").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("done")]
    [InlineData("Later")]
    [InlineData("")]
    public void Status_Unknown_IsRejected(string status)
    {
        ErrorsFor(Active() with { Status = status }, "Status")
            .ShouldBe(["Status must be one of: ToDo, InProgress, Done."]);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void Direction_AscOrDesc_Passes(string direction)
    {
        ErrorsFor(Active() with { Direction = direction }, "Direction").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ASC")]
    [InlineData("up")]
    [InlineData("")]
    public void Direction_Other_IsRejected(string direction)
    {
        ErrorsFor(Active() with { Direction = direction }, "Direction").ShouldBe(["Direction must be asc or desc."]);
    }

    [Fact]
    public void DueRange_OfExactly90Days_Passes()
    {
        _validator.Validate(Active() with { DueFrom = "2026-01-01", DueTo = "2026-04-01" }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DueRange_OfOneDay_Passes()
    {
        _validator.Validate(Active() with { DueFrom = "2026-01-01", DueTo = "2026-01-01" }).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DueRange_Of91Days_IsRejectedOnDueTo()
    {
        ErrorsFor(Active() with { DueFrom = "2026-01-01", DueTo = "2026-04-02" }, "DueTo")
            .ShouldBe(["Choose a range of at most 90 days."]);
    }

    [Fact]
    public void DueRange_UsesTheConfiguredMaximum()
    {
        var validator = new ListTasksValidator(Options.Create(new TaskListOptions { DueDateRangeMaxDays = 30 }));
        var within = Active() with { DueFrom = "2026-01-01", DueTo = "2026-01-31" };
        var beyond = Active() with { DueFrom = "2026-01-01", DueTo = "2026-02-01" };

        validator.Validate(within).IsValid.ShouldBeTrue();
        validator.Validate(beyond).Errors.Select(error => error.ErrorMessage)
            .ShouldBe(["Choose a range of at most 30 days."]);
    }

    [Fact]
    public void DueRange_Inverted_IsRejectedOnDueTo()
    {
        ErrorsFor(Active() with { DueFrom = "2026-02-01", DueTo = "2026-01-31" }, "DueTo")
            .ShouldBe(["The start date must be on or before the end date."]);
    }

    [Theory]
    [InlineData("2026-01-01", null, "DueTo")]
    [InlineData(null, "2026-01-01", "DueFrom")]
    public void DueRange_WithOnlyOneEnd_IsRejectedOnTheMissingEnd(string? dueFrom, string? dueTo, string missing)
    {
        ErrorsFor(Active() with { DueFrom = dueFrom, DueTo = dueTo }, missing)
            .ShouldBe(["Choose both a start and an end date."]);
    }

    [Theory]
    [InlineData("2026-1-01", "2026-01-31", "DueFrom")]
    [InlineData("2026-01-01", "31/01/2026", "DueTo")]
    [InlineData("2026-02-30", "2026-03-01", "DueFrom")]
    public void DueRange_WithAnInvalidDate_IsRejectedOnThatDate(string dueFrom, string dueTo, string invalid)
    {
        ErrorsFor(Active() with { DueFrom = dueFrom, DueTo = dueTo }, invalid).ShouldBe(["Enter dates as YYYY-MM-DD."]);
    }

    [Fact]
    public void InvalidDates_DoNotAlsoReportTheRangeRules()
    {
        var result = _validator.Validate(Active() with { DueFrom = "nope", DueTo = "2020-01-01" });

        result.Errors.Select(error => error.ErrorMessage).ShouldBe(["Enter dates as YYYY-MM-DD."]);
    }

    private string[] ErrorsFor(ListTasksQuery query, string propertyName) =>
        [.. _validator.Validate(query).Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)];
}
