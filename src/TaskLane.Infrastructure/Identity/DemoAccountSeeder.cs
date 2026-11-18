using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TaskLane.Application.Identity.Ports;
using TaskLane.Domain.Identity;

namespace TaskLane.Infrastructure.Identity;

/// <summary>
/// Creates the documented demo account if no account uses its email. Idempotent: an existing
/// account is never modified, so restarts do not duplicate it or reset its password (spec FR-016).
/// </summary>
internal sealed class DemoAccountSeeder(
    IOptions<DemoAccountOptions> options,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var demo = options.Value;
        if (!demo.IsConfigured)
        {
            return;
        }

        if (await users.GetByNormalizedEmailAsync(User.NormalizeEmail(demo.Email), cancellationToken) is not null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        await users.TryAddAsync(
            User.Register(Guid.CreateVersion7(now), demo.Email, demo.DisplayName, passwordHasher.Hash(demo.Password), now),
            cancellationToken);
    }
}

public static class DemoAccountProvisioning
{
    /// <summary>Provisions the demo account. Call only in local development.</summary>
    public static async Task SeedDemoAccountAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<DemoAccountSeeder>().SeedAsync(cancellationToken).ConfigureAwait(false);
    }
}
