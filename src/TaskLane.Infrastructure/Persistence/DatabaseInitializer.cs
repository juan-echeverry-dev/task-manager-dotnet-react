using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TaskLane.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations. Intended for local development and demos only;
    /// production deployments should apply migrations as an explicit release step.
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
