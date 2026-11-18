using Microsoft.EntityFrameworkCore;

using TaskLane.Domain.Identity;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Identity;

public sealed class UserPersistenceTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavedUser_RoundTripsAllFields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);
        var user = User.Register(Guid.CreateVersion7(), " Ana@Example.com ", "Ana", "hash-value", CreatedAt);

        await using (var context = database.CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = database.CreateContext();
        var stored = await readContext.Users.SingleAsync(cancellationToken);
        stored.Id.ShouldBe(user.Id);
        stored.Email.ShouldBe("Ana@Example.com");
        stored.NormalizedEmail.ShouldBe("ana@example.com");
        stored.DisplayName.ShouldBe("Ana");
        stored.PasswordHash.ShouldBe("hash-value");
        stored.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public async Task SavingTwoUsersWithSameNormalizedEmail_IsRejectedByTheDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteDatabase.CreateAsync(cancellationToken);

        await using (var context = database.CreateContext())
        {
            context.Users.Add(User.Register(Guid.CreateVersion7(), "ana@example.com", "Ana", "hash", CreatedAt));
            await context.SaveChangesAsync(cancellationToken);
        }

        await using var secondContext = database.CreateContext();
        secondContext.Users.Add(User.Register(Guid.CreateVersion7(), "ANA@example.com", "Ana 2", "hash", CreatedAt));

        await Should.ThrowAsync<DbUpdateException>(() => secondContext.SaveChangesAsync(cancellationToken));
    }
}
