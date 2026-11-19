using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.CreateTask;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks.CreateTask;

public sealed class CreateTaskHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly CreateTaskHandler _handler;

    public CreateTaskHandlerTests()
    {
        _handler = new CreateTaskHandler(new CreateTaskValidator(), _tasks, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task ValidCommand_StoresTaskAndReturnsIt()
    {
        TaskItem? stored = null;
        await _tasks.AddAsync(Arg.Do<TaskItem>(task => stored = task), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(
            new CreateTaskCommand(OwnerId, "  Prepare demo  ", "Slides", "InProgress", "2026-12-01"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        stored.ShouldNotBeNull();
        stored.OwnerId.ShouldBe(OwnerId);
        stored.Id.Version.ShouldBe(7);
        stored.Title.ShouldBe("Prepare demo");
        stored.Description.ShouldBe("Slides");
        stored.Status.ShouldBe(TaskItemStatus.InProgress);
        stored.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
        stored.CreatedAt.ShouldBe(Now);
        result.Value.ShouldBe(TaskResponse.From(stored));
    }

    [Fact]
    public async Task OnlyTitle_DefaultsStatusToToDoWithoutDueDate()
    {
        var result = await _handler.HandleAsync(
            new CreateTaskCommand(OwnerId, "Prepare demo", null, null, null),
            TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe(TaskItemStatus.ToDo);
        result.Value.Description.ShouldBeNull();
        result.Value.DueDate.ShouldBeNull();
    }

    [Fact]
    public async Task InvalidCommand_ReturnsValidationErrorWithoutSaving()
    {
        var result = await _handler.HandleAsync(
            new CreateTaskCommand(OwnerId, " ", new string('d', 2001), "Later", "2100-01-01"),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["Title", "Description", "Status", "DueDate"], ignoreOrder: true);
        await _tasks.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }
}
