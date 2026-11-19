using Microsoft.EntityFrameworkCore;

using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.Ports;
using TaskLane.Domain.Tasks;
using TaskLane.Infrastructure.Persistence;

namespace TaskLane.Infrastructure.Tasks;

/// <summary>EF Core adapter for <see cref="ITaskRepository"/>. Every query starts from the owner's tasks.</summary>
internal sealed class TaskRepository(AppDbContext context) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        context.Tasks.Add(task);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TaskItem>> ListAsync(Guid ownerId, TaskListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tasks = context.Tasks.AsNoTracking()
            .Where(task => task.OwnerId == ownerId)
            .Where(task => query.Deleted ? task.DeletedAt != null : task.DeletedAt == null);

        var totalCount = await tasks.CountAsync(cancellationToken);

        var items = await Order(tasks)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskItem>(items, query.Page, query.PageSize, totalCount);
    }

    // Tasks without a due date always come last; ties go to the most recently created, then the id.
    private static IOrderedQueryable<TaskItem> Order(IQueryable<TaskItem> tasks) =>
        tasks
            .OrderBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id);
}
