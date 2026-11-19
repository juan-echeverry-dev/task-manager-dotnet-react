using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.GetTask;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.GetTask;

public sealed class GetTaskHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly GetTaskHandler _handler;

    public GetTaskHandlerTests()
    {
        _handler = new GetTaskHandler(_tasks);
    }

    [Fact]
    public async Task OwnedTask_IsReturned()
    {
        var task = TaskItem.Create(Guid.CreateVersion7(), OwnerId, "Prepare demo", null, null, null, Now);
        _tasks.GetOwnedAsync(task.Id, OwnerId, Arg.Any<CancellationToken>()).Returns(task);

        var result = await _handler.HandleAsync(new GetTaskQuery(OwnerId, task.Id), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(TaskResponse.From(task));
    }

    [Fact]
    public async Task MissingOrForeignTask_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(
            new GetTaskQuery(OwnerId, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
        result.Error.Detail.ShouldBe("The task does not exist.");
    }
}
