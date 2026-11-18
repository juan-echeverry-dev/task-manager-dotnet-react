using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using TaskLane.Domain.Identity;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Tests.Support;

namespace TaskLane.Infrastructure.Tests.Identity;

public sealed class DemoAccountSeederTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    private static readonly DemoAccountOptions Demo = new()
    {
        Email = "demo@tasklane.dev",
        Password = "TaskLane-Demo-2026",
        DisplayName = "Demo User",
    };

    private readonly PasswordHasherAdapter _hasher = new();
    private SqliteDatabase _database = null!;

    public async ValueTask InitializeAsync() =>
        _database = await SqliteDatabase.CreateAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task Seed_EmptyDatabase_CreatesTheDemoAccount()
    {
        await SeedAsync(Demo);

        var user = await SingleUserAsync();
        user.Email.ShouldBe("demo@tasklane.dev");
        user.DisplayName.ShouldBe("Demo User");
        user.CreatedAt.ShouldBe(Now);
        _hasher.Verify(user.PasswordHash, "TaskLane-Demo-2026").ShouldBeTrue();
    }

    [Fact]
    public async Task Seed_Twice_KeepsOneAccountWithTheSamePasswordHash()
    {
        await SeedAsync(Demo);
        var firstHash = (await SingleUserAsync()).PasswordHash;

        await SeedAsync(Demo);

        (await SingleUserAsync()).PasswordHash.ShouldBe(firstHash);
    }

    [Fact]
    public async Task Seed_ExistingAccountWithDemoEmail_IsLeftUnchanged()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var context = _database.CreateContext())
        {
            context.Users.Add(User.Register(
                Guid.CreateVersion7(), "DEMO@tasklane.dev", "Someone", _hasher.Hash("another password"), Now));
            await context.SaveChangesAsync(cancellationToken);
        }

        await SeedAsync(Demo);

        var user = await SingleUserAsync();
        user.DisplayName.ShouldBe("Someone");
        _hasher.Verify(user.PasswordHash, "another password").ShouldBeTrue();
    }

    [Fact]
    public async Task Seed_WithoutConfiguration_DoesNothing()
    {
        await SeedAsync(new DemoAccountOptions());

        await using var context = _database.CreateContext();
        (await context.Users.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    private async Task SeedAsync(DemoAccountOptions options)
    {
        await using var context = _database.CreateContext();
        var seeder = new DemoAccountSeeder(
            Options.Create(options), new UserRepository(context), _hasher, new FakeTimeProvider(Now));

        await seeder.SeedAsync(TestContext.Current.CancellationToken);
    }

    private async Task<User> SingleUserAsync()
    {
        await using var context = _database.CreateContext();
        return await context.Users.SingleAsync(TestContext.Current.CancellationToken);
    }
}
