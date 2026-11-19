using TaskLane.Application.Tasks.ListTasks;

namespace TaskLane.UnitTests.Tasks.ListTasks;

public sealed class ListTasksValidatorTests
{
    private readonly ListTasksValidator _validator = new();

    private static ListTasksQuery Active() =>
        new(Guid.CreateVersion7(), Deleted: false, null, null, null, null, null, null, null, null);

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
    public void Sort_KnownField_PassesInBothLists(string sort)
    {
        ErrorsFor(Active() with { Sort = sort }, "Sort").ShouldBeEmpty();
        ErrorsFor(Active() with { Deleted = true, Sort = sort }, "Sort").ShouldBeEmpty();
    }

    [Fact]
    public void Sort_DeletedAt_PassesOnlyInTheDeletedList()
    {
        ErrorsFor(Active() with { Deleted = true, Sort = "deletedAt" }, "Sort").ShouldBeEmpty();
        ErrorsFor(Active() with { Sort = "deletedAt" }, "Sort")
            .ShouldBe(["Sort must be one of: dueDate, createdAt, title."]);
    }

    [Theory]
    [InlineData("DueDate")]
    [InlineData("priority")]
    [InlineData("")]
    public void Sort_Unknown_IsRejectedWithTheFieldsOfThatList(string sort)
    {
        ErrorsFor(Active() with { Sort = sort }, "Sort")
            .ShouldBe(["Sort must be one of: dueDate, createdAt, title."]);
        ErrorsFor(Active() with { Deleted = true, Sort = sort }, "Sort")
            .ShouldBe(["Sort must be one of: dueDate, createdAt, title, deletedAt."]);
    }

    private string[] ErrorsFor(ListTasksQuery query, string propertyName) =>
        [.. _validator.Validate(query).Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)];
}
