using Microsoft.EntityFrameworkCore;

using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Tasks;

public sealed class TaskPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavedTask_RoundTripsAllFields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        var owner = await AddUserAsync(database, cancellationToken);
        var task = TaskItem.Create(
            Guid.CreateVersion7(), owner.Id, "Prepare demo", "Slides", TaskItemStatus.InProgress,
            new DateOnly(2026, 12, 1), Now.AddTicks(1234567));
        task.Update("Prepare demo", "Slides and seed data", TaskItemStatus.Done, new DateOnly(2026, 12, 2), Now.AddMinutes(30));
        task.Delete(Now.AddHours(1));

        await using (var context = database.CreateContext())
        {
            context.Tasks.Add(task);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = database.CreateContext();
        var saved = await readContext.Tasks.SingleAsync(cancellationToken);
        saved.Id.ShouldBe(task.Id);
        saved.OwnerId.ShouldBe(owner.Id);
        saved.Title.ShouldBe("Prepare demo");
        saved.Description.ShouldBe("Slides and seed data");
        saved.Status.ShouldBe(TaskItemStatus.Done);
        saved.DueDate.ShouldBe(new DateOnly(2026, 12, 2));
        saved.CreatedAt.ShouldBe(Now.AddTicks(1234567));
        saved.UpdatedAt.ShouldBe(Now.AddMinutes(30));
        saved.DeletedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public async Task SavingTaskWithUnknownOwner_FailsOnForeignKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        context.Tasks.Add(NewTask(Guid.CreateVersion7()));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task DeletingOwner_CascadesToTasks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        var owner = await AddUserAsync(database, cancellationToken);
        await using (var context = database.CreateContext())
        {
            context.Tasks.Add(NewTask(owner.Id));
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var context = database.CreateContext())
        {
            await context.Users.Where(user => user.Id == owner.Id).ExecuteDeleteAsync(cancellationToken);
        }

        await using var readContext = database.CreateContext();
        (await readContext.Tasks.CountAsync(cancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Timestamps_CanBeOrderedInQueries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        var owner = await AddUserAsync(database, cancellationToken);
        var older = NewTask(owner.Id, Now);
        var newer = NewTask(owner.Id, Now.AddMinutes(5));
        older.Delete(Now.AddHours(2));
        newer.Delete(Now.AddHours(1));
        await using (var context = database.CreateContext())
        {
            context.Tasks.AddRange(older, newer);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = database.CreateContext();
        var byCreated = await readContext.Tasks.OrderByDescending(task => task.CreatedAt).Select(task => task.Id).ToListAsync(cancellationToken);
        var byDeleted = await readContext.Tasks.OrderByDescending(task => task.DeletedAt).Select(task => task.Id).ToListAsync(cancellationToken);

        byCreated.ShouldBe([newer.Id, older.Id]);
        byDeleted.ShouldBe([older.Id, newer.Id]);
    }

    private static TaskItem NewTask(Guid ownerId, DateTimeOffset? createdAt = null) =>
        TaskItem.Create(Guid.CreateVersion7(), ownerId, "Task", null, null, null, createdAt ?? Now);

    private static async Task<User> AddUserAsync(SqliteDatabase database, CancellationToken cancellationToken)
    {
        var user = User.Register(Guid.CreateVersion7(), "ana@example.com", "Ana", "hash", Now);
        await using var context = database.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
