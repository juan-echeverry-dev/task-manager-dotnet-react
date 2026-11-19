using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.DeleteTask;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.UnitTests.Tasks.DeleteTask;

public sealed class DeleteTaskHandlerTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();
    private static readonly Guid TaskId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly DeleteTaskHandler _handler;

    public DeleteTaskHandlerTests() => _handler = new DeleteTaskHandler(_tasks);

    [Fact]
    public async Task OwnedTask_IsRemovedPermanently()
    {
        _tasks.DeleteOwnedAsync(TaskId, OwnerId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(new DeleteTaskCommand(OwnerId, TaskId), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(TaskId);
        await _tasks.Received(1).DeleteOwnedAsync(TaskId, OwnerId, Arg.Any<CancellationToken>());
        await _tasks.DidNotReceiveWithAnyArgs().GetOwnedAsync(default, default, TestContext.Current.CancellationToken);
        await _tasks.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MissingForeignOrAlreadyDeletedTask_ReturnsNotFound()
    {
        _tasks.DeleteOwnedAsync(TaskId, OwnerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(new DeleteTaskCommand(OwnerId, TaskId), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
        result.Error.Detail.ShouldBe("The task does not exist.");
    }
}
