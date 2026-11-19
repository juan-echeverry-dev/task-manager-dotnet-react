using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Application.Tasks.UpdateTask;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.UpdateTask;

public sealed class UpdateTaskHandlerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = CreatedAt.AddHours(1);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly UpdateTaskHandler _handler;
    private readonly TaskItem _task = TaskItem.Create(
        Guid.CreateVersion7(), OwnerId, "Prepare demo", "Slides", TaskItemStatus.ToDo, new DateOnly(2026, 12, 1), CreatedAt);

    public UpdateTaskHandlerTests()
    {
        _tasks.GetOwnedAsync(_task.Id, OwnerId, Arg.Any<CancellationToken>()).Returns(_task);
        _tasks.UpdateAsync(_task, Arg.Any<CancellationToken>()).Returns(true);
        _handler = new UpdateTaskHandler(new UpdateTaskValidator(), _tasks, new FakeTimeProvider(Now));
    }

    private UpdateTaskCommand Command(string status = "Done", string? description = "New description", string? dueDate = "2027-01-15") =>
        new(OwnerId, _task.Id, " New title ", description, status, dueDate);

    [Fact]
    public async Task ValidCommand_UpdatesAndSavesTheTask()
    {
        var result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _task.Title.ShouldBe("New title");
        _task.Description.ShouldBe("New description");
        _task.Status.ShouldBe(TaskItemStatus.Done);
        _task.DueDate.ShouldBe(new DateOnly(2027, 1, 15));
        _task.UpdatedAt.ShouldBe(Now);
        await _tasks.Received(1).UpdateAsync(_task, Arg.Any<CancellationToken>());
        result.Value.ShouldBe(TaskResponse.From(_task));
    }

    [Fact]
    public async Task NullDescriptionAndDueDate_ClearThem()
    {
        await _handler.HandleAsync(Command(description: null, dueDate: null), TestContext.Current.CancellationToken);

        _task.Description.ShouldBeNull();
        _task.DueDate.ShouldBeNull();
    }

    [Fact]
    public async Task MissingOrForeignTask_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(
            Command() with { TaskId = Guid.CreateVersion7() }, TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
        result.Error.Detail.ShouldBe("The task does not exist.");
        await _tasks.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TaskDeletedWhileSaving_ReturnsNotFound()
    {
        _tasks.UpdateAsync(_task, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Title.ShouldBe("Task not found");
    }

    [Fact]
    public async Task DeletedTask_ReturnsConflictWithoutChanges()
    {
        _task.Delete(CreatedAt.AddMinutes(30));

        var result = await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Title.ShouldBe("Task is deleted");
        result.Error.Detail.ShouldBe("Restore the task before editing it.");
        _task.Title.ShouldBe("Prepare demo");
        await _tasks.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InvalidCommand_ReturnsValidationErrorWithoutLookingUpTheTask()
    {
        var result = await _handler.HandleAsync(Command(status: "Later"), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["Status"]);
        await _tasks.DidNotReceiveWithAnyArgs().GetOwnedAsync(default, default, TestContext.Current.CancellationToken);
    }
}
