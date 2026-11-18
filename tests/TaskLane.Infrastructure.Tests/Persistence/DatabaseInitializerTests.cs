using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tests.Persistence;

public sealed class DatabaseInitializerTests : IAsyncDisposable
{
    // A single open connection keeps the in-memory SQLite database alive for the whole test.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;

    public DatabaseInitializerTests()
    {
        _connection.Open();
        _services = new ServiceCollection()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(_connection))
            .BuildServiceProvider();
    }

    [Fact]
    public async Task InitializeDatabaseAsync_LeavesNoPendingMigrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _services.InitializeDatabaseAsync(cancellationToken);

        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.Database.CanConnectAsync(cancellationToken)).ShouldBeTrue();
        (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ShouldBeEmpty();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
