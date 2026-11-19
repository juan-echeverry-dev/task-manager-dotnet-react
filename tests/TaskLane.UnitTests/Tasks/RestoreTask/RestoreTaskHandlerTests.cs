using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Application.Tasks.RestoreTask;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.RestoreTask;

public sealed class RestoreTaskHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly RestoreTaskHandler _handler;
    private readonly TaskItem _task = TaskItem.Create(
        Guid.CreateVersion7(), OwnerId, "Prepare demo", "Slides", TaskItemStatus.InProgress, new DateOnly(2026, 12, 1), CreatedAt);

    public RestoreTaskHandlerTests()
    {
        _tasks.GetOwnedAsync(_task.Id, OwnerId, Arg.Any<CancellationToken>()).Returns(_task);
        _handler = new RestoreTaskHandler(_tasks);
    }

    [Fact]
    public async Task DeletedTask_IsRestoredWithItsValuesAndSaved()
    {
        _task.Delete(CreatedAt.AddHours(1));

        var result = await _handler.HandleAsync(new RestoreTaskCommand(OwnerId, _task.Id), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(TaskResponse.From(_task));
        result.Value.DeletedAt.ShouldBeNull();
        result.Value.UpdatedAt.ShouldBe(CreatedAt);
        result.Value.Title.ShouldBe("Prepare demo");
        result.Value.Status.ShouldBe(TaskItemStatus.InProgress);
        await _tasks.Received(1).UpdateAsync(_task, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActiveTask_SucceedsWithoutChanges()
    {
        var result = await _handler.HandleAsync(new RestoreTaskCommand(OwnerId, _task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task MissingOrForeignTask_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(
            new RestoreTaskCommand(OwnerId, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
        await _tasks.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }
}
