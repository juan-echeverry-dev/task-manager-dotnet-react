using TaskLane.Application.Tasks;
using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks;

public sealed class TaskResponseTests
{
    [Fact]
    public void From_CopiesEveryFieldExceptTheOwner()
    {
        var now = new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
        var task = TaskItem.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Prepare demo", "Slides", TaskItemStatus.InProgress,
            new DateOnly(2026, 12, 1), now);
        task.Update("Prepare demo", "Slides", TaskItemStatus.Done, new DateOnly(2026, 12, 1), now.AddMinutes(30));
        task.Delete(now.AddHours(1));

        var response = TaskResponse.From(task);

        response.ShouldBe(new TaskResponse(
            task.Id, "Prepare demo", "Slides", TaskItemStatus.Done, new DateOnly(2026, 12, 1),
            now, now.AddMinutes(30), now.AddHours(1)));
    }
}
