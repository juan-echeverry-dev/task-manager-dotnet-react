using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TaskLane.Application.Identity.Ports;
using TaskLane.Domain.Identity;
using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Identity;

internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        var entry = dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique })
        {
            // Leave the context usable for the rest of the request.
            entry.State = EntityState.Detached;
            return false;
        }
    }

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
}
