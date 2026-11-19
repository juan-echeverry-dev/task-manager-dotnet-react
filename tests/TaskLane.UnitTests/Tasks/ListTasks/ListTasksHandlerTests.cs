using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.ListTasks;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.ListTasks;

public sealed class ListTasksHandlerTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ListTasksHandler _handler;

    public ListTasksHandlerTests()
    {
        _tasks.ListAsync(Arg.Any<Guid>(), Arg.Any<TaskListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskItem>([], 1, 20, 0));
        _handler = new ListTasksHandler(new ListTasksValidator(), _tasks);
    }

    private static ListTasksQuery Active() =>
        new(OwnerId, Deleted: false, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task NoParameters_UsesTheDefaultQueryForTheOwner()
    {
        await _handler.HandleAsync(Active(), TestContext.Current.CancellationToken);

        await _tasks.Received(1).ListAsync(OwnerId, TaskListQuery.Default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Paging_IsPassedToTheRepository()
    {
        await _handler.HandleAsync(Active() with { Page = 3, PageSize = 10 }, TestContext.Current.CancellationToken);

        await _tasks.Received(1).ListAsync(
            OwnerId, TaskListQuery.Default with { Page = 3, PageSize = 10 }, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsThePageAsTaskResponses()
    {
        var task = TaskItem.Create(
            Guid.CreateVersion7(), OwnerId, "Prepare demo", null, null, null,
            new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
        _tasks.ListAsync(OwnerId, Arg.Any<TaskListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskItem>([task], 2, 10, 11));

        var result = await _handler.HandleAsync(Active() with { Page = 2, PageSize = 10 }, TestContext.Current.CancellationToken);

        result.Value.Items.ShouldBe([TaskResponse.From(task)]);
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(10);
        result.Value.TotalCount.ShouldBe(11);
    }

    [Fact]
    public async Task InvalidQuery_ReturnsValidationErrorWithoutQuerying()
    {
        var result = await _handler.HandleAsync(Active() with { PageSize = 101 }, TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["PageSize"]);
        await _tasks.DidNotReceiveWithAnyArgs().ListAsync(default, default!, TestContext.Current.CancellationToken);
    }
}
