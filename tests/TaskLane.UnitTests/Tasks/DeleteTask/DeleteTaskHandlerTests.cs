using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks.DeleteTask;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.DeleteTask;

public sealed class DeleteTaskHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = CreatedAt.AddHours(1);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly DeleteTaskHandler _handler;
    private readonly TaskItem _task = TaskItem.Create(Guid.CreateVersion7(), OwnerId, "Prepare demo", null, null, null, CreatedAt);

    public DeleteTaskHandlerTests()
    {
        _tasks.GetOwnedAsync(_task.Id, OwnerId, Arg.Any<CancellationToken>()).Returns(_task);
        _handler = new DeleteTaskHandler(_tasks, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task ActiveTask_IsDeletedAndSaved()
    {
        var result = await _handler.HandleAsync(new DeleteTaskCommand(OwnerId, _task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _task.DeletedAt.ShouldBe(Now);
        _task.UpdatedAt.ShouldBe(CreatedAt);
        await _tasks.Received(1).UpdateAsync(_task, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlreadyDeletedTask_SucceedsWithoutChanges()
    {
        _task.Delete(CreatedAt.AddMinutes(5));

        var result = await _handler.HandleAsync(new DeleteTaskCommand(OwnerId, _task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _task.DeletedAt.ShouldBe(CreatedAt.AddMinutes(5));
    }

    [Fact]
    public async Task MissingOrForeignTask_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(
            new DeleteTaskCommand(OwnerId, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
        await _tasks.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }
}
