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

        var items = await Order(tasks, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskItem>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<TaskItem?> GetOwnedAsync(Guid id, Guid ownerId, CancellationToken cancellationToken) =>
        context.Tasks.SingleOrDefaultAsync(task => task.Id == id && task.OwnerId == ownerId, cancellationToken);

    // The task was loaded (and is tracked) by GetOwnedAsync in the same scope.
    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IOrderedQueryable<TaskItem> Order(IQueryable<TaskItem> tasks, TaskListQuery query)
    {
        var descending = query.Direction == TaskSortDirection.Descending;

        var ordered = query.Sort switch
        {
            // Tasks without a due date always come last, whatever the direction.
            TaskSortField.DueDate => descending
                ? tasks.OrderBy(task => task.DueDate == null).ThenByDescending(task => task.DueDate)
                : tasks.OrderBy(task => task.DueDate == null).ThenBy(task => task.DueDate),
            TaskSortField.CreatedAt => descending
                ? tasks.OrderByDescending(task => task.CreatedAt)
                : tasks.OrderBy(task => task.CreatedAt),
            // Case-insensitive for ASCII letters (SQLite NOCASE collation), as for search.
            TaskSortField.Title => descending
                ? tasks.OrderByDescending(task => EF.Functions.Collate(task.Title, "NOCASE"))
                : tasks.OrderBy(task => EF.Functions.Collate(task.Title, "NOCASE")),
            TaskSortField.DeletedAt => descending
                ? tasks.OrderByDescending(task => task.DeletedAt)
                : tasks.OrderBy(task => task.DeletedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Sort, "Unknown sort field."),
        };

        // Ties go to the most recently created task, then the id, so paging is stable.
        return ordered.ThenByDescending(task => task.CreatedAt).ThenBy(task => task.Id);
    }
}
