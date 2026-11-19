using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tests.Persistence;

/// <summary>
/// Rolling out permanent deletion removes the tasks already soft-deleted and the column that marked
/// them, and leaves every other task unchanged (spec 003 US3, FR-006, SC-004). Reads use plain SQL so
/// the test does not depend on the current EF model.
/// </summary>
public sealed class RemoveTaskSoftDeleteMigrationTests : IAsyncLifetime
{
    private const string OwnerId = "0193A5C8-0000-7000-8000-000000000001";
    private const string ActiveId = "0193A5C8-0000-7000-8000-0000000000A1";
    private const string DeletedId = "0193A5C8-0000-7000-8000-0000000000D1";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _connection.OpenAsync(CancellationToken);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task Migration_RemovesSoftDeletedTasksAndTheColumn_KeepsActiveTasksUnchanged()
    {
        await MigrateAsync("AddTasks");
        await ExecuteAsync($"""
            INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "DisplayName", "PasswordHash", "CreatedAt")
            VALUES ('{OwnerId}', 'ana@example.com', 'ANA@EXAMPLE.COM', 'Ana', 'hash', '2026-11-18 09:00:00+00:00');
            INSERT INTO "Tasks" ("Id", "OwnerId", "Title", "Description", "Status", "DueDate", "CreatedAt", "UpdatedAt", "DeletedAt")
            VALUES ('{ActiveId}', '{OwnerId}', 'Keep me', 'Slides', 'InProgress', '2026-12-01', 1000, 2000, NULL),
                   ('{DeletedId}', '{OwnerId}', 'Deleted before rollout', NULL, 'ToDo', NULL, 1000, 1000, 3000);
            """);

        await MigrateAsync(null);

        var rows = await QueryAsync(
            """SELECT "Id", "Title", "Description", "Status", "DueDate", "CreatedAt", "UpdatedAt" FROM "Tasks" """);
        rows.ShouldBe([[ActiveId, "Keep me", "Slides", "InProgress", "2026-12-01", "1000", "2000"]]);

        var columns = await QueryAsync("""SELECT "name" FROM pragma_table_info('Tasks')""");
        columns.Select(column => column[0]).ShouldNotContain("DeletedAt");

        var indexes = await QueryAsync("""SELECT "name" FROM pragma_index_list('Tasks')""");
        indexes.Select(index => index[0]).ShouldContain("IX_Tasks_OwnerId_DueDate");
        indexes.Select(index => index[0]).ShouldContain("IX_Tasks_OwnerId_CreatedAt");
        indexes.Select(index => index[0]).ShouldNotContain("IX_Tasks_OwnerId_DeletedAt_DueDate");
        indexes.Select(index => index[0]).ShouldNotContain("IX_Tasks_OwnerId_DeletedAt_CreatedAt");
    }

    private async Task MigrateAsync(string? targetMigration)
    {
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        await context.GetService<IMigrator>().MigrateAsync(targetMigration, CancellationToken);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private async Task<List<string?[]>> QueryAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        var rows = new List<string?[]>();
        while (await reader.ReadAsync(CancellationToken))
        {
            var row = new string?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[index] = reader.IsDBNull(index) ? null : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
            }

            rows.Add(row);
        }

        return rows;
    }
}
