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

        var tasks = Filter(
            context.Tasks.AsNoTracking()
                .Where(task => task.OwnerId == ownerId)
                .Where(task => query.Deleted ? task.DeletedAt != null : task.DeletedAt == null),
            query);

        var totalCount = await tasks.CountAsync(cancellationToken);

        // Computed as long: (page - 1) * pageSize can exceed int for large page numbers. A page past
        // the end has no items, so it needs no query; otherwise the offset is below totalCount and fits.
        var offset = (long)(query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return new PagedResult<TaskItem>([], query.Page, query.PageSize, totalCount);
        }

        var items = await Order(tasks, query)
            .Skip((int)offset)
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

    private static IQueryable<TaskItem> Filter(IQueryable<TaskItem> tasks, TaskListQuery query)
    {
        if (query.Search is { } search)
        {
            // "Contains", case-insensitive for ASCII letters (SQLite LIKE); wildcards in the text are literal.
            var pattern = $"%{EscapeLike(search)}%";
            tasks = tasks.Where(task =>
                EF.Functions.Like(task.Title, pattern, LikeEscape)
                || (task.Description != null && EF.Functions.Like(task.Description, pattern, LikeEscape)));
        }

        if (query.Status is { } status)
        {
            tasks = tasks.Where(task => task.Status == status);
        }

        // Inclusive on both ends; tasks without a due date never match a range.
        if (query.DueFrom is { } dueFrom && query.DueTo is { } dueTo)
        {
            tasks = tasks.Where(task => task.DueDate != null && task.DueDate >= dueFrom && task.DueDate <= dueTo);
        }

        return tasks;
    }

    private const string LikeEscape = "\\";

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

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
