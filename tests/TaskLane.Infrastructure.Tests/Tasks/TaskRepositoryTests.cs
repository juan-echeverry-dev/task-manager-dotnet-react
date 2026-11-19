using Microsoft.EntityFrameworkCore;

using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Tasks;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Tasks;

public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private SqliteDatabase _database = null!;
    private Guid _owner;
    private Guid _otherOwner;

    private static readonly TaskListQuery DeletedDefault =
        TaskListQuery.Default with { Deleted = true, Sort = TaskSortField.DeletedAt, Direction = SortDirection.Descending };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await SqliteDatabase.CreateAsync(CancellationToken);
        _owner = await AddUserAsync("ana@example.com");
        _otherOwner = await AddUserAsync("bob@example.com");
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task AddAsync_PersistsTheTask()
    {
        var task = NewTask(_owner, "Prepare demo");

        await using (var context = _database.CreateContext())
        {
            await new TaskRepository(context).AddAsync(task, CancellationToken);
        }

        await using var readContext = _database.CreateContext();
        (await readContext.Tasks.SingleAsync(CancellationToken)).Id.ShouldBe(task.Id);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyTheOwnersActiveTasks()
    {
        var mine = NewTask(_owner, "Mine");
        var deleted = NewTask(_owner, "Deleted");
        deleted.Delete(Now);
        await SaveAsync(mine, deleted, NewTask(_otherOwner, "Theirs"));

        var page = await ListAsync(TaskListQuery.Default);

        page.Items.Select(task => task.Id).ShouldBe([mine.Id]);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_DefaultOrder_DueDateAscendingNoDueDateLastThenNewestCreatedFirst()
    {
        var noDateOld = NewTask(_owner, "No date, old", createdAt: Now);
        var noDateNew = NewTask(_owner, "No date, new", createdAt: Now.AddMinutes(1));
        var december = NewTask(_owner, "December", new DateOnly(2026, 12, 1), Now);
        var januaryOld = NewTask(_owner, "January, old", new DateOnly(2027, 1, 1), Now);
        var januaryNew = NewTask(_owner, "January, new", new DateOnly(2027, 1, 1), Now.AddMinutes(1));
        var past = NewTask(_owner, "Past", new DateOnly(2026, 1, 1), Now);
        await SaveAsync(noDateOld, noDateNew, december, januaryOld, januaryNew, past);

        var page = await ListAsync(TaskListQuery.Default);

        page.Items.Select(task => task.Title).ShouldBe(
            ["Past", "December", "January, new", "January, old", "No date, new", "No date, old"]);
    }

    [Fact]
    public async Task ListAsync_SameDueDateAndCreatedAt_IsOrderedById()
    {
        var first = TaskItem.Create(new Guid("00000000-0000-7000-8000-000000000001"), _owner, "A", null, null, null, Now);
        var second = TaskItem.Create(new Guid("00000000-0000-7000-8000-000000000002"), _owner, "B", null, null, null, Now);
        await SaveAsync(second, first);

        var page = await ListAsync(TaskListQuery.Default);

        page.Items.Select(task => task.Id).ShouldBe([first.Id, second.Id]);
    }

    [Fact]
    public async Task ListAsync_ReturnsTheRequestedPageAndTotal()
    {
        var tasks = Enumerable.Range(1, 25)
            .Select(day => NewTask(_owner, $"Task {day:00}", new DateOnly(2026, 12, day), Now))
            .ToArray();
        await SaveAsync(tasks);

        var page = await ListAsync(TaskListQuery.Default with { Page = 3, PageSize = 10 });

        page.Items.Select(task => task.Title).ShouldBe(["Task 21", "Task 22", "Task 23", "Task 24", "Task 25"]);
        page.Page.ShouldBe(3);
        page.PageSize.ShouldBe(10);
        page.TotalCount.ShouldBe(25);
    }

    [Fact]
    public async Task ListAsync_PagePastTheEnd_ReturnsNoItemsWithTheRealTotal()
    {
        await SaveAsync(NewTask(_owner, "Only"));

        var page = await ListAsync(TaskListQuery.Default with { Page = 5 });

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_DeletedList_ReturnsOnlyTheOwnersDeletedTasksMostRecentlyDeletedFirst()
    {
        var active = NewTask(_owner, "Active");
        var deletedFirst = NewTask(_owner, "Deleted first");
        var deletedLast = NewTask(_owner, "Deleted last");
        var theirs = NewTask(_otherOwner, "Theirs");
        deletedFirst.Delete(Now.AddHours(1));
        deletedLast.Delete(Now.AddHours(2));
        theirs.Delete(Now.AddHours(3));
        await SaveAsync(active, deletedFirst, deletedLast, theirs);

        var page = await ListAsync(DeletedDefault);

        page.Items.Select(task => task.Title).ShouldBe(["Deleted last", "Deleted first"]);
        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_ActiveListAndCount_ExcludeDeletedTasks()
    {
        var deleted = NewTask(_owner, "Deleted");
        deleted.Delete(Now);
        await SaveAsync(NewTask(_owner, "One"), NewTask(_owner, "Two"), deleted);

        var page = await ListAsync(TaskListQuery.Default);

        page.Items.Select(task => task.Title).ShouldNotContain("Deleted");
        page.TotalCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetOwnedAsync_ReturnsTheOwnersTaskActiveOrDeleted(bool deleted)
    {
        var task = NewTask(_owner, "Mine");
        if (deleted)
        {
            task.Delete(Now);
        }

        await SaveAsync(task);

        await using var context = _database.CreateContext();
        var found = await new TaskRepository(context).GetOwnedAsync(task.Id, _owner, CancellationToken);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(task.Id);
        found.IsDeleted.ShouldBe(deleted);
    }

    [Fact]
    public async Task GetOwnedAsync_ForAnotherOwnersTaskOrUnknownId_ReturnsNull()
    {
        var theirs = NewTask(_otherOwner, "Theirs");
        await SaveAsync(theirs);

        await using var context = _database.CreateContext();
        var repository = new TaskRepository(context);

        (await repository.GetOwnedAsync(theirs.Id, _owner, CancellationToken)).ShouldBeNull();
        (await repository.GetOwnedAsync(Guid.CreateVersion7(), _owner, CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesToALoadedTask()
    {
        var task = NewTask(_owner, "Old", new DateOnly(2026, 12, 1));
        await SaveAsync(task);

        await using (var context = _database.CreateContext())
        {
            var repository = new TaskRepository(context);
            var loaded = await repository.GetOwnedAsync(task.Id, _owner, CancellationToken);
            loaded!.Update("New", "Description", TaskItemStatus.Done, null, Now.AddHours(1));
            await repository.UpdateAsync(loaded, CancellationToken);
        }

        await using var readContext = _database.CreateContext();
        var saved = await readContext.Tasks.SingleAsync(CancellationToken);
        saved.Title.ShouldBe("New");
        saved.Description.ShouldBe("Description");
        saved.Status.ShouldBe(TaskItemStatus.Done);
        saved.DueDate.ShouldBeNull();
        saved.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    private async Task<Application.Tasks.PagedResult<TaskItem>> ListAsync(TaskListQuery query, Guid? ownerId = null)
    {
        await using var context = _database.CreateContext();
        return await new TaskRepository(context).ListAsync(ownerId ?? _owner, query, CancellationToken);
    }

    private static TaskItem NewTask(
        Guid ownerId, string title, DateOnly? dueDate = null, DateTimeOffset? createdAt = null) =>
        TaskItem.Create(Guid.CreateVersion7(), ownerId, title, null, null, dueDate, createdAt ?? Now);

    private async Task SaveAsync(params TaskItem[] tasks)
    {
        await using var context = _database.CreateContext();
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync(CancellationToken);
    }

    private async Task<Guid> AddUserAsync(string email)
    {
        var user = User.Register(Guid.CreateVersion7(), email, "Person", "hash", Now);
        await using var context = _database.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken);
        return user.Id;
    }
}
