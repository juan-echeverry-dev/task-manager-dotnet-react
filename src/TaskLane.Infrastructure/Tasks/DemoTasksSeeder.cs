using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TaskLane.Domain.Identity;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tasks;

/// <summary>
/// Gives the demo account six sample tasks: every status, an overdue task, a finished task with a past
/// date, and a task without a due date. Runs only when the account has no tasks at all, deleted ones
/// included, so restarts never duplicate them or undo what a presenter did (spec, Assumptions).
/// </summary>
internal sealed class DemoTasksSeeder(
    IOptions<DemoAccountOptions> options,
    AppDbContext context,
    TimeProvider timeProvider)
{
    private static readonly SampleTask[] Samples =
    [
        new("Prepare the TaskLane demo", "Walk through creating, finding and restoring tasks.", TaskItemStatus.InProgress, 2),
        new("Review pull request feedback", "Answer the open review comments.", TaskItemStatus.ToDo, -3),
        new("Write the release notes", null, TaskItemStatus.ToDo, 7),
        new("Book a room for the retrospective", null, TaskItemStatus.Done, -5),
        new("Update the project README", "Document the task screens and settings.", TaskItemStatus.InProgress, 14),
        new("Try the keyboard shortcuts", null, TaskItemStatus.ToDo, null),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var demo = options.Value;
        if (!demo.IsConfigured)
        {
            return;
        }

        var normalizedEmail = User.NormalizeEmail(demo.Email);
        var ownerId = await context.Users
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (ownerId is not { } demoId || await context.Tasks.AnyAsync(task => task.OwnerId == demoId, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        for (var index = 0; index < Samples.Length; index++)
        {
            var sample = Samples[index];
            // Older samples first, one minute apart, so "newest first" orders are predictable.
            var createdAt = now.AddMinutes(index - Samples.Length + 1);
            context.Tasks.Add(TaskItem.Create(
                Guid.CreateVersion7(createdAt),
                demoId,
                sample.Title,
                sample.Description,
                sample.Status,
                sample.DueInDays is { } days ? today.AddDays(days) : null,
                createdAt));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record SampleTask(string Title, string? Description, TaskItemStatus Status, int? DueInDays);
}

public static class DemoTasksProvisioning
{
    /// <summary>Provisions the demo account's sample tasks. Call only in local development, after the account.</summary>
    public static async Task SeedDemoTasksAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<DemoTasksSeeder>().SeedAsync(cancellationToken).ConfigureAwait(false);
    }
}
