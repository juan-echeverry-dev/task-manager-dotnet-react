using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tests.Support;

/// <summary>
/// A private in-memory SQLite database with all migrations applied. The open connection keeps the
/// database alive; every context created from it sees the same data.
/// </summary>
internal sealed class SqliteDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    private SqliteDatabase()
    {
    }

    public static async Task<SqliteDatabase> CreateAsync(CancellationToken cancellationToken)
    {
        var database = new SqliteDatabase();
        await database._connection.OpenAsync(cancellationToken);

        await using var context = database.CreateContext();
        await context.Database.MigrateAsync(cancellationToken);

        return database;
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
