using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Tasks;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Tasks;

public sealed class DemoTasksSeederTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 18);

    private static readonly DemoAccountOptions Demo = new()
    {
        Email = "demo@tasklane.dev",
        Password = "TaskLane-Demo-2026",
        DisplayName = "Demo User",
    };

    private SqliteDatabase _database = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _database = await SqliteDatabase.CreateAsync(CancellationToken);

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task Seed_DemoAccountWithoutTasks_CreatesSixVariedTasks()
    {
        var demoId = await AddUserAsync("DEMO@tasklane.dev");

        await SeedAsync(Demo);

        var tasks = await TasksAsync();
        tasks.Count.ShouldBe(6);
        tasks.ShouldAllBe(task => task.OwnerId == demoId && task.DeletedAt == null);
        tasks.Select(task => task.Status).Distinct().ShouldBe(
            [TaskItemStatus.ToDo, TaskItemStatus.InProgress, TaskItemStatus.Done], ignoreOrder: true);
        tasks.ShouldContain(task => task.Status == TaskItemStatus.ToDo && task.DueDate == Today.AddDays(-3));
        tasks.ShouldContain(task => task.DueDate == null);
        tasks.ShouldContain(task => task.Status == TaskItemStatus.Done && task.DueDate < Today);
        tasks.ShouldAllBe(task => task.CreatedAt <= Now);
        tasks.Select(task => task.CreatedAt).Distinct().Count().ShouldBe(6);
    }

    [Fact]
    public async Task Seed_Twice_CreatesNothingMore()
    {
        await AddUserAsync(Demo.Email);
        await SeedAsync(Demo);

        await SeedAsync(Demo);

        (await TasksAsync()).Count.ShouldBe(6);
    }

    [Fact]
    public async Task Seed_AfterEveryDemoTaskWasDeleted_CreatesThemAgain()
    {
        // Deletion is permanent (spec 003, Assumptions): an account with no tasks left is seeded again.
        await AddUserAsync(Demo.Email);
        await SeedAsync(Demo);
        await using (var context = _database.CreateContext())
        {
            await context.Tasks.ExecuteDeleteAsync(CancellationToken);
        }

        await SeedAsync(Demo);

        (await TasksAsync()).Count.ShouldBe(6);
    }

    [Fact]
    public async Task Seed_NeverModifiesAnEditedDemoTask()
    {
        await AddUserAsync(Demo.Email);
        await SeedAsync(Demo);
        Guid editedId;
        await using (var context = _database.CreateContext())
        {
            var task = await context.Tasks.FirstAsync(CancellationToken);
            task.Update("Edited by the presenter", null, TaskItemStatus.Done, null, Now.AddHours(1));
            await context.SaveChangesAsync(CancellationToken);
            editedId = task.Id;
        }

        await SeedAsync(Demo);

        var edited = (await TasksAsync()).Single(task => task.Id == editedId);
        edited.Title.ShouldBe("Edited by the presenter");
        edited.Status.ShouldBe(TaskItemStatus.Done);
    }

    [Fact]
    public async Task Seed_WithoutTheDemoAccount_CreatesNothing()
    {
        await AddUserAsync("someone@example.com");

        await SeedAsync(Demo);

        (await TasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Seed_WithoutDemoConfiguration_CreatesNothing()
    {
        await AddUserAsync(Demo.Email);

        await SeedAsync(new DemoAccountOptions());

        (await TasksAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Seed_DoesNotTouchOtherAccounts()
    {
        await AddUserAsync(Demo.Email);
        var otherId = await AddUserAsync("someone@example.com");

        await SeedAsync(Demo);

        (await TasksAsync()).ShouldAllBe(task => task.OwnerId != otherId);
    }

    private async Task SeedAsync(DemoAccountOptions options)
    {
        await using var context = _database.CreateContext();
        var seeder = new DemoTasksSeeder(Options.Create(options), context, new FakeTimeProvider(Now));
        await seeder.SeedAsync(CancellationToken);
    }

    private async Task<List<TaskItem>> TasksAsync()
    {
        await using var context = _database.CreateContext();
        return await context.Tasks.ToListAsync(CancellationToken);
    }

    private async Task<Guid> AddUserAsync(string email)
    {
        var user = User.Register(Guid.CreateVersion7(), email, "Person", "hash", Now.AddDays(-1));
        await using var context = _database.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken);
        return user.Id;
    }
}
