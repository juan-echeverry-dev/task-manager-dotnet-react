using System.Diagnostics;

using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Tasks;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Tasks;

/// <summary>Search, filters and sort orders of <see cref="TaskRepository.ListAsync"/> (spec US4).</summary>
public sealed class TaskRepositoryQueryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private SqliteDatabase _database = null!;
    private Guid _owner;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await SqliteDatabase.CreateAsync(CancellationToken);
        var user = User.Register(Guid.CreateVersion7(), "ana@example.com", "Ana", "hash", Now);
        await using var context = _database.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken);
        _owner = user.Id;
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Search_MatchesTitleOrDescriptionIgnoringAsciiCase()
    {
        await SaveAsync(
            Task("Prepare DEMO"),
            Task("Slides", description: "for the demo day"),
            Task("Unrelated", description: "nothing here"));

        var page = await ListAsync(TaskListQuery.Default with { Search = "Demo", Sort = TaskSortField.Title });

        Titles(page).ShouldBe(["Prepare DEMO", "Slides"]);
        page.TotalCount.ShouldBe(2);
    }

    [Theory]
    [InlineData("100%", "Done 100%")]
    [InlineData("a_b", "a_b literal")]
    [InlineData(@"c:\temp", @"Path c:\temp")]
    public async Task Search_TreatsWildcardsAndEscapesLiterally(string search, string expected)
    {
        await SaveAsync(Task("Done 100%"), Task("Done 1000"), Task("a_b literal"), Task("axb other"), Task(@"Path c:\temp"));

        var page = await ListAsync(TaskListQuery.Default with { Search = search });

        Titles(page).ShouldBe([expected]);
    }

    [Fact]
    public async Task StatusFilter_ReturnsOnlyThatStatus()
    {
        await SaveAsync(Task("To do"), Task("Busy", status: TaskItemStatus.InProgress), Task("Finished", status: TaskItemStatus.Done));

        var page = await ListAsync(TaskListQuery.Default with { Status = TaskItemStatus.InProgress });

        Titles(page).ShouldBe(["Busy"]);
    }

    [Fact]
    public async Task DueRange_IsInclusiveAndExcludesTasksWithoutADueDate()
    {
        await SaveAsync(
            Task("Before", dueDate: new DateOnly(2026, 11, 30)),
            Task("First day", dueDate: new DateOnly(2026, 12, 1)),
            Task("Middle", dueDate: new DateOnly(2026, 12, 15)),
            Task("Last day", dueDate: new DateOnly(2026, 12, 31)),
            Task("After", dueDate: new DateOnly(2027, 1, 1)),
            Task("No date"));

        var page = await ListAsync(TaskListQuery.Default with
        {
            DueFrom = new DateOnly(2026, 12, 1),
            DueTo = new DateOnly(2026, 12, 31),
        });

        Titles(page).ShouldBe(["First day", "Middle", "Last day"]);
    }

    [Fact]
    public async Task AllFilters_ApplyTogether()
    {
        await SaveAsync(
            Task("Demo match", status: TaskItemStatus.InProgress, dueDate: new DateOnly(2026, 12, 5)),
            Task("Demo wrong status", status: TaskItemStatus.Done, dueDate: new DateOnly(2026, 12, 5)),
            Task("Demo out of range", status: TaskItemStatus.InProgress, dueDate: new DateOnly(2027, 2, 1)),
            Task("Other match", status: TaskItemStatus.InProgress, dueDate: new DateOnly(2026, 12, 5)));

        var page = await ListAsync(TaskListQuery.Default with
        {
            Search = "demo",
            Status = TaskItemStatus.InProgress,
            DueFrom = new DateOnly(2026, 12, 1),
            DueTo = new DateOnly(2026, 12, 31),
        });

        Titles(page).ShouldBe(["Demo match"]);
    }

    [Theory]
    [InlineData(TaskSortDirection.Ascending, new[] { "Dec 1", "Dec 2", "Dec 3", "No date" })]
    [InlineData(TaskSortDirection.Descending, new[] { "Dec 3", "Dec 2", "Dec 1", "No date" })]
    public async Task SortByDueDate_KeepsTasksWithoutADueDateLast(TaskSortDirection direction, string[] expected)
    {
        await SaveAsync(
            Task("No date"),
            Task("Dec 2", dueDate: new DateOnly(2026, 12, 2)),
            Task("Dec 1", dueDate: new DateOnly(2026, 12, 1)),
            Task("Dec 3", dueDate: new DateOnly(2026, 12, 3)));

        var page = await ListAsync(TaskListQuery.Default with { Direction = direction });

        Titles(page).ShouldBe(expected);
    }

    [Theory]
    [InlineData(TaskSortDirection.Ascending, new[] { "First", "Second", "Third" })]
    [InlineData(TaskSortDirection.Descending, new[] { "Third", "Second", "First" })]
    public async Task SortByCreatedAt_FollowsTheDirection(TaskSortDirection direction, string[] expected)
    {
        await SaveAsync(Task("Second", createdAt: Now.AddMinutes(1)), Task("Third", createdAt: Now.AddMinutes(2)), Task("First"));

        var page = await ListAsync(TaskListQuery.Default with { Sort = TaskSortField.CreatedAt, Direction = direction });

        Titles(page).ShouldBe(expected);
    }

    [Theory]
    [InlineData(TaskSortDirection.Ascending, new[] { "apple", "Banana", "cherry" })]
    [InlineData(TaskSortDirection.Descending, new[] { "cherry", "Banana", "apple" })]
    public async Task SortByTitle_IgnoresCase(TaskSortDirection direction, string[] expected)
    {
        await SaveAsync(Task("cherry"), Task("apple"), Task("Banana"));

        var page = await ListAsync(TaskListQuery.Default with { Sort = TaskSortField.Title, Direction = direction });

        Titles(page).ShouldBe(expected);
    }

    [Theory]
    [InlineData(TaskSortDirection.Ascending, new[] { "Deleted first", "Deleted last" })]
    [InlineData(TaskSortDirection.Descending, new[] { "Deleted last", "Deleted first" })]
    public async Task SortByDeletedAt_FollowsTheDirection(TaskSortDirection direction, string[] expected)
    {
        var first = Task("Deleted first");
        var last = Task("Deleted last");
        first.Delete(Now.AddHours(1));
        last.Delete(Now.AddHours(2));
        await SaveAsync(first, last);

        var page = await ListAsync(TaskListQuery.Default with
        {
            Deleted = true,
            Sort = TaskSortField.DeletedAt,
            Direction = direction,
        });

        Titles(page).ShouldBe(expected);
    }

    [Fact]
    public async Task Ties_AreBrokenByNewestCreatedThenId()
    {
        var olderA = TaskItem.Create(new Guid("00000000-0000-7000-8000-00000000000a"), _owner, "Same", null, null, null, Now);
        var olderB = TaskItem.Create(new Guid("00000000-0000-7000-8000-00000000000b"), _owner, "Same", null, null, null, Now);
        var newer = TaskItem.Create(Guid.CreateVersion7(), _owner, "Same", null, null, null, Now.AddMinutes(1));
        await SaveAsync(olderB, newer, olderA);

        var page = await ListAsync(TaskListQuery.Default with { Sort = TaskSortField.Title });

        page.Items.Select(task => task.Id).ShouldBe([newer.Id, olderA.Id, olderB.Id]);
    }

    [Fact]
    public async Task DeletedList_SupportsTheSameFilters()
    {
        var match = Task("Demo deleted", status: TaskItemStatus.Done, dueDate: new DateOnly(2026, 12, 10));
        var otherStatus = Task("Demo deleted too", dueDate: new DateOnly(2026, 12, 10));
        var active = Task("Demo active", status: TaskItemStatus.Done, dueDate: new DateOnly(2026, 12, 10));
        match.Delete(Now);
        otherStatus.Delete(Now);
        await SaveAsync(match, otherStatus, active);

        var page = await ListAsync(TaskListQuery.Default with
        {
            Deleted = true,
            Sort = TaskSortField.DeletedAt,
            Direction = TaskSortDirection.Descending,
            Search = "demo",
            Status = TaskItemStatus.Done,
            DueFrom = new DateOnly(2026, 12, 1),
            DueTo = new DateOnly(2026, 12, 31),
        });

        Titles(page).ShouldBe(["Demo deleted"]);
    }

    [Fact]
    public async Task FilteredSortedPage_Of500Tasks_IsReturnedQuickly()
    {
        // A sanity check for SC-004, not a production latency test; the budget is generous on purpose.
        var tasks = Enumerable.Range(0, 500)
            .Select(index => Task(
                $"Task {index:000} {(index % 5 == 0 ? "demo" : "other")}",
                status: (TaskItemStatus)(index % 3),
                dueDate: index % 7 == 0 ? null : new DateOnly(2026, 1, 1).AddDays(index % 365),
                createdAt: Now.AddMinutes(index)))
            .ToArray();
        await SaveAsync(tasks);
        var query = TaskListQuery.Default with
        {
            Search = "demo",
            Status = TaskItemStatus.ToDo,
            Sort = TaskSortField.Title,
            Page = 2,
            PageSize = 10,
        };
        await ListAsync(query); // warm-up: model building and query compilation

        var stopwatch = Stopwatch.StartNew();
        var page = await ListAsync(query);
        stopwatch.Stop();

        page.TotalCount.ShouldBe(tasks.Count(task => task.Title.Contains("demo", StringComparison.Ordinal) && task.Status == TaskItemStatus.ToDo));
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    private TaskItem Task(
        string title,
        string? description = null,
        TaskItemStatus status = TaskItemStatus.ToDo,
        DateOnly? dueDate = null,
        DateTimeOffset? createdAt = null) =>
        TaskItem.Create(Guid.CreateVersion7(), _owner, title, description, status, dueDate, createdAt ?? Now);

    private static string[] Titles(PagedResult<TaskItem> page) => [.. page.Items.Select(task => task.Title)];

    private async Task<PagedResult<TaskItem>> ListAsync(TaskListQuery query)
    {
        await using var context = _database.CreateContext();
        return await new TaskRepository(context).ListAsync(_owner, query, CancellationToken);
    }

    private async Task SaveAsync(params TaskItem[] tasks)
    {
        await using var context = _database.CreateContext();
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync(CancellationToken);
    }
}
