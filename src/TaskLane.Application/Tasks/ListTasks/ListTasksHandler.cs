using FluentValidation;

using TaskLane.Application.Common.Results;
using TaskLane.Application.Common.Validation;
using TaskLane.Application.Tasks.Ports;

namespace TaskLane.Application.Tasks.ListTasks;

/// <summary>Lists the signed-in person's tasks, one page at a time (spec FR-016 to FR-018).</summary>
public sealed class ListTasksHandler(IValidator<ListTasksQuery> validator, ITaskRepository tasks)
{
    public async Task<Result<PagedResult<TaskResponse>>> HandleAsync(ListTasksQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<PagedResult<TaskResponse>>(validation.ToError());
        }

        var listQuery = TaskListQuery.Default with
        {
            Page = query.Page ?? 1,
            PageSize = query.PageSize ?? TaskListQuery.DefaultPageSize,
        };

        var page = await tasks.ListAsync(query.OwnerId, listQuery, cancellationToken);

        return Result.Success(new PagedResult<TaskResponse>(
            [.. page.Items.Select(TaskResponse.From)], page.Page, page.PageSize, page.TotalCount));
    }
}
