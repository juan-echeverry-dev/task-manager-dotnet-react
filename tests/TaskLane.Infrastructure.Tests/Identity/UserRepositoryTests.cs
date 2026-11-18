using Microsoft.EntityFrameworkCore;

using TaskLane.Domain.Identity;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Identity;

public sealed class UserRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TryAddAsync_NewEmail_PersistsUserAndReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        var user = NewUser("ana@example.com");

        await using (var context = database.CreateContext())
        {
            (await new UserRepository(context).TryAddAsync(user, cancellationToken)).ShouldBeTrue();
        }

        await using var readContext = database.CreateContext();
        (await readContext.Users.SingleAsync(cancellationToken)).Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task TryAddAsync_DuplicateNormalizedEmail_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        await using var context = database.CreateContext();
        var repository = new UserRepository(context);
        await repository.TryAddAsync(NewUser("ana@example.com"), cancellationToken);

        var added = await repository.TryAddAsync(NewUser(" ANA@Example.com "), cancellationToken);

        added.ShouldBeFalse();
        (await context.Users.CountAsync(cancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task TryAddAsync_SameEmailFromTwoIndependentContexts_ExactlyOneSucceeds()
    {
        // Neither context checks for existence first: the unique index alone decides the race.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();

        var results = new[]
        {
            await new UserRepository(first).TryAddAsync(NewUser("ana@example.com"), cancellationToken),
            await new UserRepository(second).TryAddAsync(NewUser("ana@example.com"), cancellationToken),
        };

        results.Count(added => added).ShouldBe(1);
        await using var readContext = database.CreateContext();
        (await readContext.Users.CountAsync(cancellationToken)).ShouldBe(1);
    }

    private static User NewUser(string email) =>
        User.Register(Guid.CreateVersion7(), email, "Ana", "hash", CreatedAt);
}
