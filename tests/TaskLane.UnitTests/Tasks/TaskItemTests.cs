using TaskLane.Domain.Tasks;

namespace TaskLane.UnitTests.Tasks;

public sealed class TaskItemTests
{
    private static readonly Guid Id = Guid.CreateVersion7();
    private static readonly Guid OwnerId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddHours(2);

    private static TaskItem CreateTask(
        string title = "Prepare demo",
        string? description = null,
        TaskItemStatus? status = null,
        DateOnly? dueDate = null) =>
        TaskItem.Create(Id, OwnerId, title, description, status, dueDate, Now);

    [Fact]
    public void Create_SetsValuesAndDefaults()
    {
        var task = CreateTask();

        task.Id.ShouldBe(Id);
        task.OwnerId.ShouldBe(OwnerId);
        task.Title.ShouldBe("Prepare demo");
        task.Description.ShouldBeNull();
        task.Status.ShouldBe(TaskItemStatus.ToDo);
        task.DueDate.ShouldBeNull();
        task.CreatedAt.ShouldBe(Now);
        task.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Create_KeepsGivenValues()
    {
        var task = CreateTask("Prepare demo", "Slides and seed data", TaskItemStatus.InProgress, new DateOnly(2026, 12, 1));

        task.Description.ShouldBe("Slides and seed data");
        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [Fact]
    public void Create_TrimsTitle()
    {
        CreateTask("  Prepare demo  ").Title.ShouldBe("Prepare demo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_StoresBlankDescriptionAsNull(string description)
    {
        CreateTask(description: description).Description.ShouldBeNull();
    }

    [Fact]
    public void Create_AcceptsMaximumLengths()
    {
        var title = new string('t', TaskItem.TitleMaxLength);
        var description = new string('d', TaskItem.DescriptionMaxLength);

        var task = CreateTask($"  {title}  ", description);

        task.Title.Length.ShouldBe(200);
        task.Description!.Length.ShouldBe(2000);
    }

    [Fact]
    public void Create_AcceptsDueDateBounds()
    {
        CreateTask(dueDate: new DateOnly(2000, 1, 1)).DueDate.ShouldBe(TaskItem.MinDueDate);
        CreateTask(dueDate: new DateOnly(2099, 12, 31)).DueDate.ShouldBe(TaskItem.MaxDueDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_Throws(string title)
    {
        Should.Throw<ArgumentException>(() => CreateTask(title));
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws()
    {
        Should.Throw<ArgumentException>(() => CreateTask(new string('t', TaskItem.TitleMaxLength + 1)));
    }

    [Fact]
    public void Create_WithTooLongDescription_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            CreateTask(description: new string('d', TaskItem.DescriptionMaxLength + 1)));
    }

    [Theory]
    [InlineData(1999, 12, 31)]
    [InlineData(2100, 1, 1)]
    public void Create_WithDueDateOutOfRange_Throws(int year, int month, int day)
    {
        Should.Throw<ArgumentException>(() => CreateTask(dueDate: new DateOnly(year, month, day)));
    }

    [Fact]
    public void Create_WithUndefinedStatus_Throws()
    {
        Should.Throw<ArgumentException>(() => CreateTask(status: (TaskItemStatus)42));
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            TaskItem.Create(Guid.Empty, OwnerId, "Prepare demo", null, null, null, Now));
    }

    [Fact]
    public void Create_WithEmptyOwnerId_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            TaskItem.Create(Id, Guid.Empty, "Prepare demo", null, null, null, Now));
    }

    [Fact]
    public void Create_WithNonUtcTime_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            TaskItem.Create(Id, OwnerId, "Prepare demo", null, null, null, new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.FromHours(-5))));
    }

    [Fact]
    public void Update_ReplacesValuesAndSetsUpdatedAt()
    {
        var task = CreateTask("Old", "Old description", TaskItemStatus.ToDo, new DateOnly(2026, 12, 1));

        task.Update("  New  ", "New description", TaskItemStatus.InProgress, new DateOnly(2027, 1, 15), Later);

        task.Title.ShouldBe("New");
        task.Description.ShouldBe("New description");
        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.DueDate.ShouldBe(new DateOnly(2027, 1, 15));
        task.UpdatedAt.ShouldBe(Later);
        task.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Update_CanClearDescriptionAndDueDate()
    {
        var task = CreateTask("Title", "Description", TaskItemStatus.ToDo, new DateOnly(2026, 12, 1));

        task.Update("Title", "  ", TaskItemStatus.ToDo, null, Later);

        task.Description.ShouldBeNull();
        task.DueDate.ShouldBeNull();
    }

    [Theory]
    [InlineData(TaskItemStatus.ToDo, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.ToDo)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.ToDo)]
    public void Update_AllowsAnyStatusTransition(TaskItemStatus from, TaskItemStatus to)
    {
        var task = CreateTask(status: from);

        task.Update(task.Title, null, to, null, Later);

        task.Status.ShouldBe(to);
    }

    [Fact]
    public void Update_EnforcesSameInvariantsAsCreate()
    {
        var task = CreateTask();

        Should.Throw<ArgumentException>(() => task.Update(" ", null, TaskItemStatus.ToDo, null, Later));
        Should.Throw<ArgumentException>(() =>
            task.Update(new string('t', TaskItem.TitleMaxLength + 1), null, TaskItemStatus.ToDo, null, Later));
        Should.Throw<ArgumentException>(() =>
            task.Update("Title", new string('d', TaskItem.DescriptionMaxLength + 1), TaskItemStatus.ToDo, null, Later));
        Should.Throw<ArgumentException>(() =>
            task.Update("Title", null, TaskItemStatus.ToDo, new DateOnly(2100, 1, 1), Later));
        Should.Throw<ArgumentException>(() => task.Update("Title", null, (TaskItemStatus)42, null, Later));
        Should.Throw<ArgumentException>(() =>
            task.Update("Title", null, TaskItemStatus.ToDo, null, Later.ToOffset(TimeSpan.FromHours(2))));
    }
}
