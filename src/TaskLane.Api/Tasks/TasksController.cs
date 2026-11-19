using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using TaskLane.Api.Common;
using TaskLane.Application.Tasks;
using TaskLane.Application.Tasks.CreateTask;
using TaskLane.Application.Tasks.DeleteTask;
using TaskLane.Application.Tasks.GetTask;
using TaskLane.Application.Tasks.ListTasks;
using TaskLane.Application.Tasks.RestoreTask;
using TaskLane.Application.Tasks.UpdateTask;

namespace TaskLane.Api.Tasks;

/// <summary>
/// Task endpoints (contracts/tasks-api.md). Authentication is required by the fallback policy, and the
/// owner is always the signed-in person.
/// </summary>
[ApiController]
[Route("api/tasks")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class TasksController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public Task<IActionResult> List(
        [FromQuery] TaskListRequest request,
        [FromServices] ListTasksHandler handler,
        CancellationToken cancellationToken) =>
        ListAsync(deleted: false, request, handler, cancellationToken);

    // "deleted" and "settings" are declared before "{id:guid}"; the guid constraint also keeps them apart.
    [HttpGet("deleted")]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public Task<IActionResult> ListDeleted(
        [FromQuery] TaskListRequest request,
        [FromServices] ListTasksHandler handler,
        CancellationToken cancellationToken) =>
        ListAsync(deleted: true, request, handler, cancellationToken);

    [HttpGet("settings")]
    [ProducesResponseType<TaskSettingsResponse>(StatusCodes.Status200OK)]
    public IActionResult GetSettings([FromServices] IOptions<TaskListOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Ok(new TaskSettingsResponse(options.Value.DueDateRangeMaxDays));
    }

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create(
        CreateTaskRequest request,
        [FromServices] CreateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(
            new CreateTaskCommand(ownerId, request.Title ?? string.Empty, request.Description, request.Status, request.DueDate),
            cancellationToken);

        return result.ToActionResult(task => Created($"/api/tasks/{task.Id}", task));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromServices] GetTaskHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(new GetTaskQuery(ownerId, id), cancellationToken);

        return result.ToActionResult(Ok);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTaskRequest request,
        [FromServices] UpdateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(
            new UpdateTaskCommand(ownerId, id, request.Title ?? string.Empty, request.Description, request.Status, request.DueDate),
            cancellationToken);

        return result.ToActionResult(Ok);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteTaskHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(new DeleteTaskCommand(ownerId, id), cancellationToken);

        return result.ToActionResult(_ => NoContent());
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] RestoreTaskHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(new RestoreTaskCommand(ownerId, id), cancellationToken);

        return result.ToActionResult(Ok);
    }

    private async Task<IActionResult> ListAsync(
        bool deleted,
        TaskListRequest request,
        ListTasksHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        if (User.GetUserId() is not { } ownerId)
        {
            return InvalidSession();
        }

        var result = await handler.HandleAsync(
            new ListTasksQuery(
                ownerId,
                deleted,
                request.Search,
                request.Status,
                request.DueFrom,
                request.DueTo,
                request.Sort,
                request.Direction,
                request.Page,
                request.PageSize),
            cancellationToken);

        return result.ToActionResult(Ok);
    }

    // The token is validated by then; a subject that is not a user id is still not a session.
    private ObjectResult InvalidSession() =>
        Problem(
            title: "Authentication required",
            detail: "Your session is no longer valid. Please sign in again.",
            statusCode: StatusCodes.Status401Unauthorized);
}
