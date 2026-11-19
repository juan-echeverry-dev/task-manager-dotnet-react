---

description: "Task list for feature 002 — Task Management"
---

# Tasks: Task Management

**Input**: Design documents from `specs/002-task-management/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: REQUIRED. Constitution principle II (Test-First) applies to every task:
- Each test task is written first and MUST fail (Red) before the implementation task that follows
  it (Green). An optional refactor closes the cycle.
- Commit each step separately: `test:`, then `feat:`, then `refactor:`.
- Commits carry no co-author trailers.

**Organization**: Tasks are grouped by user story, so each story can be implemented and verified
on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: User story from spec.md (US1–US4)
- Paths are relative to the repository root.
- Shared conventions from feature 001:
  - Request DTOs have nullable fields, so validation errors come from Application.
  - Handlers return `Result<T>`, and controllers map them with `ResultMappings`.
  - Tests use `TestContext.Current.CancellationToken`.
  - Not found means `Error.NotFound("Task not found", "The task does not exist.")`.
  - Editing a deleted task returns `Error.Conflict("Task is deleted", "Restore the task before editing it.")`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuration needed before any test can be written.

- [X] T001 Add `<PackageVersion Include="Microsoft.Extensions.Options" Version="$(DotNetPackagesVersion)" />` to `Directory.Packages.props` and reference it from `src/TaskLane.Application/TaskLane.Application.csproj`. Then run `dotnet build TaskLane.slnx`: 0 warnings.
- [X] T002 [P] Add `"Tasks": { "DueDateRangeMaxDays": 90 }` to `src/TaskLane.Api/appsettings.json`. This is configuration only and is exempt from test-first.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `TaskItem` entity and table, the current-user helper, and module boundaries. Every story needs them.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [X] T003 [P] Write failing unit tests in `tests/TaskLane.UnitTests/Tasks/TaskItemTests.cs` for `TaskItem.Create(id, ownerId, title, description, status, dueDate, now)`:
  - **Defaults and normalization:** title trimmed; a description that is empty or only whitespace becomes `null`; `Status` defaults to `ToDo` when null is passed; `UpdatedAt == CreatedAt == now`; `DeletedAt == null`.
  - **Limits:** title "1–200 chars" after trim; description "≤ 2,000 chars"; due date "2000-01-01 … 2099-12-31 inclusive", with both bounds accepted and one day outside each bound rejected.
  - **`ArgumentException`** for: empty id, empty owner id, empty or whitespace title, any length or date out of range, and non-UTC `now`.
  - **`Update(title, description, status, dueDate, now)`:** replaces the four values, allows any status transition (including `Done → ToDo`), and sets `UpdatedAt = now`. It throws `InvalidOperationException` when the task is deleted.
  - **`Delete(now)`:** sets `DeletedAt`, and does nothing when called again. **`Restore()`:** clears `DeletedAt` and keeps `UpdatedAt`; does nothing when the task is active.
- [X] T004 Implement `TaskItemStatus` (`ToDo`, `InProgress`, `Done`) in `src/TaskLane.Domain/Tasks/TaskItemStatus.cs` and the sealed `TaskItem` in `src/TaskLane.Domain/Tasks/TaskItem.cs` until T003 passes:
  - Private setters and a private parameterless constructor for EF Core.
  - `IsDeleted`.
  - Constants `TitleMaxLength = 200`, `DescriptionMaxLength = 2000`, `MinDueDate = new DateOnly(2000, 1, 1)`, `MaxDueDate = new DateOnly(2099, 12, 31)`.
- [X] T005 [P] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Tasks/TaskPersistenceTests.cs`, using the `SqliteDatabase` helper:
  - A saved task round-trips all nine fields (`DueDate` as a date, timestamps exactly).
  - Saving a task whose `OwnerId` has no matching user throws `DbUpdateException` (foreign key).
  - Deleting the owning `User` row cascades to its tasks.
  - Ordering by `CreatedAt` and `DeletedAt` works in a LINQ query (proves the converter).
- [X] T006 Create `src/TaskLane.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`, then add the migration with `dotnet ef migrations add AddTasks --project src/TaskLane.Infrastructure --startup-project src/TaskLane.Api --output-dir Persistence/Migrations`. T005 must then pass. Configuration:
  - Table `Tasks`; `Id` with `ValueGeneratedNever`.
  - `OwnerId` required, with `HasOne<User>().WithMany().HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Cascade)`.
  - `Title` "NOT NULL, max 200"; `Description` "NULL, max 2000"; `Status` stored as a string, max 20.
  - `DueDate` nullable.
  - `CreatedAt`, `UpdatedAt` and `DeletedAt` with `UtcTicksConverter` (UTC ticks; see research R6).
  - Indexes `IX_Tasks_OwnerId_DeletedAt_DueDate` and `IX_Tasks_OwnerId_DeletedAt_CreatedAt`.
  - Expose `DbSet<TaskItem> Tasks` in `src/TaskLane.Infrastructure/Persistence/AppDbContext.cs`.
- [X] T007 [P] Write failing unit tests in `tests/TaskLane.Api.Tests/ClaimsPrincipalExtensionsTests.cs`: `GetUserId()` returns the Guid in the `sub` claim, and returns `null` when the claim is missing or is not a Guid.
- [X] T008 Implement `ClaimsPrincipalExtensions.GetUserId()` in `src/TaskLane.Api/Common/ClaimsPrincipalExtensions.cs` until T007 passes. Then make it a `refactor:` step: `src/TaskLane.Api/Identity/UsersController.cs` uses it, and all feature 001 tests stay green.
- [ ] T009 [P] Create `tests/TaskLane.ArchitectureTests/ModuleBoundaryTests.cs` with a module-boundary rule: types in namespaces `TaskLane.Domain.Tasks*`, `TaskLane.Application.Tasks*` and `TaskLane.Api.Tasks*` must not depend on types in `TaskLane.*.Identity*`. Infrastructure persistence configuration is exempt (research R11). Run it; it must pass, and it must fail during a temporary local experiment that references `User` from `TaskItem`, which you then revert.
- [ ] T010 [P] Add the shared types `PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)` in `src/TaskLane.Application/Tasks/PagedResult.cs` and `TaskResponse(Guid Id, string Title, string? Description, TaskItemStatus Status, DateOnly? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt)` with a static `From(TaskItem)` in `src/TaskLane.Application/Tasks/TaskResponse.cs`. Cover `From` with one unit test in `tests/TaskLane.UnitTests/Tasks/TaskResponseTests.cs`, written first.
- [ ] T011 Configure JSON so `TaskItemStatus` serializes as a string (`JsonStringEnumConverter`) and `DateOnly` as `yyyy-MM-dd`: add `.AddJsonOptions(...)` in `src/TaskLane.Api/Program.cs`. Then run `dotnet test --solution TaskLane.slnx`: everything must be green.

**Checkpoint**: The `Tasks` table exists, owner ids are read from one helper, and the module boundary is enforced.

---

## Phase 3: User Story 1 - Create and see my tasks (Priority: P1) 🎯 MVP

**Goal**: A signed-in person creates tasks and sees only their own, in a paginated list in the default order.

**Independent Test**: `POST /api/tasks` with a title returns 201 with status `ToDo`, and `GET /api/tasks` lists it. A second account's list does not include it. In the SPA, create a task from `/tasks/new` and see it at `/tasks`.

### Tests for User Story 1 (write first, must fail)

- [ ] T012 [P] [US1] Write failing validator tests in `tests/TaskLane.UnitTests/Tasks/CreateTask/CreateTaskValidatorTests.cs`, asserting each rule and exact message from data-model.md for title, description, status ("Optional; if present, one of the names") and dueDate ("Optional; `yyyy-MM-dd`; 2000-01-01 … 2099-12-31"). All invalid fields are reported together.
- [ ] T013 [P] [US1] Write failing handler tests in `tests/TaskLane.UnitTests/Tasks/CreateTask/CreateTaskHandlerTests.cs`, with an NSubstitute `ITaskRepository` and `FakeTimeProvider`:
  - A valid command → `AddAsync` receives a `TaskItem` with the given owner, trimmed title, parsed status (default `ToDo`), parsed due date, a v7 id and `CreatedAt = now`. Returns `TaskResponse`.
  - Invalid input → `Error.Validation` with field keys `Title`, `Description`, `Status`, `DueDate`, and the repository is not called.
- [ ] T014 [P] [US1] Write failing list tests in `tests/TaskLane.UnitTests/Tasks/ListTasks/ListTasksValidatorTests.cs` for paging only: `page` "≥ 1" and `pageSize` "1–100", with the data-model messages.
- [ ] T015 [P] [US1] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs`:
  - `AddAsync` persists the task.
  - `ListAsync(ownerId, TaskListQuery.Default)` returns only that owner's active tasks.
  - Default order: due date ascending, no due date last, ties by `CreatedAt` descending, then `Id`.
  - Paging returns the right slice and `TotalCount`.
  - A page past the end returns empty `Items` with the real `TotalCount`.
- [ ] T016 [P] [US1] Write failing API tests in `tests/TaskLane.Api.Tests/Tasks/CreateAndListTasksTests.cs`, using `RegisterAndSignInAsync` from feature 001:
  - `POST /api/tasks` with `{ "title": "Prepare demo" }` → 201, `Location: /api/tasks/{id}`, body per `contracts/tasks-api.md` (`status: "ToDo"`, `dueDate: null`).
  - Full body → all values echoed; `dueDate` as `"2026-12-01"`.
  - Invalid body → 400 with `errors.title`, `errors.description`, `errors.status`, `errors.dueDate`.
  - `GET /api/tasks` → `TaskPage` with the default order and `pageSize` 20.
  - A second account sees an empty list.
  - Without a token → 401.
  - `pageSize=101` → 400 `errors.pageSize`.

### Implementation for User Story 1

- [ ] T017 [P] [US1] Create the port in `src/TaskLane.Application/Tasks/Ports/ITaskRepository.cs`:
  - Methods `Task AddAsync(TaskItem, CancellationToken)` and `Task<PagedResult<TaskItem>> ListAsync(Guid ownerId, TaskListQuery query, CancellationToken)`.
  - `TaskListQuery` record (`Search`, `Status`, `DueFrom`, `DueTo`, `Sort`, `Direction`, `Page`, `PageSize`, `Deleted`) with `Default` = active, `dueDate` ascending, page 1, size 20.
  - `TaskSortField` enum (`DueDate`, `CreatedAt`, `Title`, `DeletedAt`) and `SortDirection` enum.
- [ ] T018 [US1] Implement the CreateTask slice in `src/TaskLane.Application/Tasks/CreateTask/` (`CreateTaskCommand(Guid OwnerId, string Title, string? Description, string? Status, string? DueDate)`, `CreateTaskValidator`, `CreateTaskHandler`) until T012 and T013 pass. Register it in `src/TaskLane.Application/DependencyInjection.cs`.
- [ ] T019 [US1] Implement the ListTasks slice (paging and default order only) in `src/TaskLane.Application/Tasks/ListTasks/` (`ListTasksQuery(Guid OwnerId, bool Deleted, string? Search, string? Status, string? DueFrom, string? DueTo, string? Sort, string? Direction, int? Page, int? PageSize)`, `ListTasksValidator`, `ListTasksHandler` → `PagedResult<TaskResponse>`) until T014 passes. Register it in DI.
- [ ] T020 [US1] Implement `TaskRepository.AddAsync` and `ListAsync` (owner-scoped, `DeletedAt == null`, default order, `Skip`/`Take`, `CountAsync`) in `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs` and register it scoped in `src/TaskLane.Infrastructure/DependencyInjection.cs` until T015 passes.
- [ ] T021 [US1] Implement `POST /api/tasks` and `GET /api/tasks` in `src/TaskLane.Api/Tasks/TasksController.cs` (`[Route("api/tasks")]`, owner from `User.GetUserId()`, `CreateTaskRequest` in `src/TaskLane.Api/Tasks/CreateTaskRequest.cs`, query parameters bound with `[FromQuery]`, `ProducesResponseType` attributes) until T016 passes.
- [ ] T022 [P] [US1] Write failing Vitest tests:
  - **`frontend/src/features/tasks/tasks-api.test.ts`:** `createTask` and `listTasks` send the bearer token, the JSON body and the query string.
  - **`frontend/src/features/tasks/overdue.test.ts`:** a past date and not Done → true; today → false; Done → false; no date → false.
  - **`frontend/src/shared/ui/SelectField.test.tsx` and `TextAreaField.test.tsx`:** label, `aria-invalid` and `aria-describedby`, as with `TextField`.
  - **`frontend/src/features/tasks/NewTaskPage.test.tsx`:**
    - labelled fields Title, Description, Status (To do / In progress / Done) and Due date;
    - Zod errors with the data-model messages;
    - "Creating…" disabled while pending;
    - a 400 maps errors to fields and keeps the values;
    - a network error shows the service message;
    - success navigates to `/tasks/:id` with "Task created.".
  - **`frontend/src/features/tasks/TaskListPage.test.tsx`:**
    - rows show the title link, status badge, local due date or "No due date", and an "Overdue" badge (with a fake clock);
    - the empty state "You have no tasks yet." links to `/tasks/new`;
    - pagination `nav` with `aria-current`;
    - "New task" link.
- [ ] T023 [US1] Implement the US1 SPA until T022 passes:
  - **`frontend/src/features/tasks/tasks-api.ts`:** types `Task`, `TaskPage`, `TaskStatus`; `createTask` and `listTasks`.
  - **Tasks feature:** `frontend/src/features/tasks/overdue.ts`, `schemas.ts`, `TaskForm.tsx`, `NewTaskPage.tsx`, and `TaskListPage.tsx` (active mode, page only).
  - **Shared UI:** `frontend/src/shared/ui/SelectField.tsx`, `TextAreaField.tsx`, `Pagination.tsx`.
  - **Routing:** `/tasks` and `/tasks/new` under `RequireAuth` in `frontend/src/app/routes.tsx`, and a "Tasks" link in `frontend/src/shared/layout/AppLayout.tsx` when signed in.

**Checkpoint**: US1 works end to end (API + SPA), and the full test suite is green.

---

## Phase 4: User Story 2 - View and edit a task (Priority: P1)

**Goal**: Open one of my tasks, see every detail, and change its values. Another person's task is indistinguishable from a missing one.

**Independent Test**:
- API: `GET /api/tasks/{id}` → 200 for my task; 404 for a missing task and for another person's task (identical bodies). `PUT` changes all four values, clears the description and due date when they are null, and allows `Done → ToDo`.
- SPA: edit a task and see the new values.

### Tests for User Story 2 (write first, must fail)

- [ ] T024 [P] [US2] Write failing unit tests:
  - `tests/TaskLane.UnitTests/Tasks/UpdateTask/UpdateTaskValidatorTests.cs`: same rules as create, except `status` is "Required; one of the names" with "Status is required.".
  - `tests/TaskLane.UnitTests/Tasks/UpdateTask/UpdateTaskHandlerTests.cs`:
    - valid → task updated with `UpdatedAt = now` and `UpdateAsync` called;
    - a missing task (`GetOwnedAsync` returns null) → `Error.NotFound`;
    - a deleted task → `Error.Conflict("Task is deleted", "Restore the task before editing it.")`, not saved;
    - invalid → `Error.Validation`, with no lookup.
  - `tests/TaskLane.UnitTests/Tasks/GetTask/GetTaskHandlerTests.cs`: found (active or deleted) → `TaskResponse`; missing → `Error.NotFound`.
- [ ] T025 [P] [US2] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs`:
  - `GetOwnedAsync(id, ownerId)` returns the task (deleted included).
  - It returns null for another owner's id or an unknown id.
  - `UpdateAsync` persists the changes.
- [ ] T026 [P] [US2] Write failing API tests in `tests/TaskLane.Api.Tests/Tasks/GetAndUpdateTaskTests.cs`:
  - GET 200.
  - PUT 200 with the new values, then GET shows them.
  - PUT with `description: null, dueDate: null` clears them.
  - `Done → ToDo` allowed.
  - PUT invalid → 400.
  - **Ownership matrix:** GET and PUT of another account's task → 404 whose body equals the 404 for a random id (SC-002).

### Implementation for User Story 2

- [ ] T027 [P] [US2] Add `Task<TaskItem?> GetOwnedAsync(Guid id, Guid ownerId, CancellationToken)` (tracked) and `Task UpdateAsync(TaskItem, CancellationToken)` to `src/TaskLane.Application/Tasks/Ports/ITaskRepository.cs` and implement them in `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs` until T025 passes.
- [ ] T028 [US2] Implement `src/TaskLane.Application/Tasks/GetTask/` (`GetTaskQuery(Guid OwnerId, Guid TaskId)`, `GetTaskHandler`) and `src/TaskLane.Application/Tasks/UpdateTask/` (`UpdateTaskCommand(Guid OwnerId, Guid TaskId, string Title, string? Description, string? Status, string? DueDate)`, `UpdateTaskValidator`, `UpdateTaskHandler`) until T024 passes. Share the title, description and due-date rules with create through `src/TaskLane.Application/Tasks/TaskFieldRules.cs` (a deviation from the rule of three, justified in plan.md Complexity Tracking). Register both in DI.
- [ ] T029 [US2] Add `GET /api/tasks/{id:guid}` and `PUT /api/tasks/{id:guid}` (`UpdateTaskRequest` in `src/TaskLane.Api/Tasks/UpdateTaskRequest.cs`) to `src/TaskLane.Api/Tasks/TasksController.cs` until T026 passes.
- [ ] T030 [P] [US2] Write failing Vitest tests:
  - **`frontend/src/features/tasks/TaskDetailPage.test.tsx`:**
    - shows title, description, status, due date, created and updated (local format);
    - "Overdue" badge when it applies;
    - "Edit" and "Delete" actions for an active task;
    - a 404 shows "Task not found" with a link to `/tasks`.
  - **`frontend/src/features/tasks/EditTaskPage.test.tsx`:**
    - form pre-filled with the task;
    - can clear the description and due date;
    - "Saving…" disabled while pending;
    - a 400 maps to fields;
    - a 404 shows "Task not found" with a link to `/tasks`;
    - a network error shows "We couldn't reach the service. Please try again." and keeps the values;
    - success → `/tasks/:id` with "Task updated.".
- [ ] T031 [US2] Implement `getTask` and `updateTask` in `frontend/src/features/tasks/tasks-api.ts`, plus `frontend/src/features/tasks/TaskDetailPage.tsx` (active mode) and `frontend/src/features/tasks/EditTaskPage.tsx` (reusing `TaskForm`). Add `/tasks/:id` and `/tasks/:id/edit` routes in `frontend/src/app/routes.tsx`. T030 must then pass.

**Checkpoint**: US1 and US2 work together: create, list, view and edit, with ownership enforced.

---

## Phase 5: User Story 3 - Delete and restore a task (Priority: P2)

**Goal**: Soft delete with confirmation, a deleted-tasks list, a read-only detail for deleted tasks, and restore. Edits to deleted tasks are refused.

**Independent Test**: Delete a task. It is no longer in `GET /api/tasks` and appears in `GET /api/tasks/deleted` (most recently deleted first). `PUT` on it returns 409. Restore it, and it is back with its values unchanged.

### Tests for User Story 3 (write first, must fail)

- [ ] T032 [P] [US3] Write failing unit tests in `tests/TaskLane.UnitTests/Tasks/DeleteTask/DeleteTaskHandlerTests.cs` and `tests/TaskLane.UnitTests/Tasks/RestoreTask/RestoreTaskHandlerTests.cs`:
  - Delete: sets `DeletedAt = now` and saves; already deleted → success with no change.
  - Restore: clears `DeletedAt`, keeps `UpdatedAt`, returns `TaskResponse`; already active → success.
  - Both: missing → `Error.NotFound`.
- [ ] T033 [P] [US3] Extend `tests/TaskLane.UnitTests/Tasks/ListTasks/ListTasksValidatorTests.cs` (failing first): when `Deleted = true`, `sort` accepts `deletedAt` and defaults to `deletedAt desc`; when `Deleted = false`, `sort=deletedAt` is rejected with "Sort must be one of: dueDate, createdAt, title.".
- [ ] T034 [P] [US3] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs`:
  - `ListAsync` with `Deleted = true` returns only the owner's deleted tasks, in `DeletedAt desc` order by default.
  - Active lists and counts exclude deleted tasks (FR-013).
- [ ] T035 [P] [US3] Write failing API tests in `tests/TaskLane.Api.Tests/Tasks/DeleteAndRestoreTaskTests.cs`:
  - `DELETE` → 204, and a second `DELETE` → 204.
  - The task is gone from `GET /api/tasks` and listed in `GET /api/tasks/deleted` with `deletedAt`.
  - `GET /api/tasks/{id}` still 200 with `deletedAt` set.
  - `PUT` → 409 Problem Details with the contract's title and detail, and the values are unchanged.
  - `POST /api/tasks/{id}/restore` → 200 with all values as before deletion (SC-003).
  - **Ownership matrix:** `DELETE` and restore of another account's task → 404 identical to a random id.

### Implementation for User Story 3

- [ ] T036 [US3] Implement `src/TaskLane.Application/Tasks/DeleteTask/` (`DeleteTaskCommand`, `DeleteTaskHandler` returning a non-generic `Result`) and `src/TaskLane.Application/Tasks/RestoreTask/` (`RestoreTaskCommand`, `RestoreTaskHandler`). Extend the ListTasks validator and handler with the deleted mode until T032 and T033 pass. Register them in DI.
- [ ] T037 [US3] Extend `TaskRepository.ListAsync` with the deleted mode and the `DeletedAt` sort in `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs` until T034 passes.
- [ ] T038 [US3] Add `DELETE /api/tasks/{id:guid}`, `POST /api/tasks/{id:guid}/restore` and `GET /api/tasks/deleted` to `src/TaskLane.Api/Tasks/TasksController.cs`. Declare `deleted` before `{id:guid}` so the routes don't clash. Add the overload `ToActionResult(this Result result, Func<IActionResult> onSuccess)` to `src/TaskLane.Api/Common/ResultMappings.cs` for the 204 response. T035 must then pass (204 on success, 404 on error).
- [ ] T039 [P] [US3] Write failing Vitest tests:
  - **`frontend/src/shared/ui/ConfirmDialog.test.tsx`:**
    - opens as a modal dialog with a title, message, "Delete" and "Cancel";
    - Escape or Cancel closes it without confirming;
    - focus returns to the trigger.
  - **`frontend/src/features/tasks/TaskDetailPage.test.tsx`:**
    - Delete opens the confirmation;
    - confirming → `/tasks` with "Task deleted."; cancel → nothing changes;
    - a deleted task is read-only, marked "Deleted on {date}", with only the "Restore" action, which shows "Task restored." and the active detail;
    - while the request is pending, the dialog's "Delete" button and the "Restore" button are disabled with `aria-busy="true"` (FR-021).
  - **`frontend/src/features/tasks/EditTaskPage.test.tsx`:** a 409 → navigates to the read-only detail with "This task was deleted. Restore it to edit."; opening edit for a task that is already deleted redirects to its detail.
  - **`frontend/src/features/tasks/TaskListPage.test.tsx`:** in deleted mode the rows show the deletion date, the empty state is "No deleted tasks.", and there is a link between "Tasks" and "Deleted tasks".
- [ ] T040 [US3] Implement the US3 SPA until T039 passes:
  - `deleteTask`, `restoreTask` and `listDeletedTasks` in `frontend/src/features/tasks/tasks-api.ts`.
  - `frontend/src/shared/ui/ConfirmDialog.tsx`, using the native `<dialog>` with `showModal`.
  - The deleted read-only mode in `frontend/src/features/tasks/TaskDetailPage.tsx` and 409 handling in `EditTaskPage.tsx`.
  - A `mode` prop in `TaskListPage.tsx`, and the `/tasks/deleted` route in `frontend/src/app/routes.tsx`.

**Checkpoint**: The delete and restore round trip works end to end, and deleted tasks cannot be edited.

---

## Phase 6: User Story 4 - Find tasks (Priority: P2)

**Goal**: Text search, status filter, due-date range filter (maximum span configurable, 90 days by default), sort field and direction, and pagination, all reflected in the URL. Both lists support them.

**Independent Test**: With tasks of varied titles, statuses and due dates, each parameter of `GET /api/tasks` and `GET /api/tasks/deleted` returns the expected subset and order. A 91-day range returns 400 and a 90-day range returns 200. Restarting with `Tasks__DueDateRangeMaxDays=30` changes the limit and the settings endpoint. In the SPA, the same state survives a reload.

### Tests for User Story 4 (write first, must fail)

- [ ] T041 [P] [US4] Extend `tests/TaskLane.UnitTests/Tasks/ListTasks/ListTasksValidatorTests.cs` (failing first) with every list rule and message from data-model.md:
  - `search` "≤ 200 after trim".
  - `status` names.
  - `dueFrom`/`dueTo`: both or neither; `yyyy-MM-dd`; start ≤ end; span ≤ `TaskListOptions.DueDateRangeMaxDays`. Test with options set to 90 (2026-01-01→2026-04-01 accepted, →2026-04-02 rejected with "Choose a range of at most 90 days.") and set to 30, to prove the limit is configurable.
  - `sort`.
  - `direction` "asc or desc"; when missing, the handler uses the sort field's default (`dueDate` asc, `title` asc, `createdAt` desc, `deletedAt` desc).
- [ ] T042 [P] [US4] Extend `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs` (failing first):
  - **Search:** over title and description, case-insensitive for ASCII, with `%` and `_` treated literally.
  - **Filters:** by status; inclusive due-date range that excludes tasks without a due date; all filters combined.
  - **Order:** every sort field and direction; nulls last for `dueDate` in both directions; tie-breakers.
  - **Both lists:** the same filters apply in deleted mode.
  - **Performance:** 500 tasks for one owner; run one warm-up query, then time only the filtered and sorted page query with `Stopwatch` and assert under 1 s. This is a sanity check for SC-004, not a production latency test; the budget is deliberately generous so it does not flake in CI.
- [ ] T043 [P] [US4] Write failing API tests in `tests/TaskLane.Api.Tests/Tasks/FindTasksTests.cs`:
  - Each query parameter end to end.
  - Errors keyed by parameter (`errors.dueTo`, `errors.sort`, `errors.status`, `errors.direction`).
  - `GET /api/tasks/settings` → `{ "dueDateRangeMaxDays": 90 }`.
  - A second factory with `Tasks:DueDateRangeMaxDays=30` → settings returns 30, and a 31-day range → 400.
  - A factory with `Tasks:DueDateRangeMaxDays=0` fails to start with `OptionsValidationException` (as the feature 001 JWT key test).

### Implementation for User Story 4

- [ ] T044 [US4] Add `TaskListOptions` (`DueDateRangeMaxDays`, default 90, valid 1–3650, section `Tasks`) in `src/TaskLane.Application/Tasks/TaskListOptions.cs`. Bind it with validation (1–3650) and `ValidateOnStart` in `src/TaskLane.Api/Program.cs`; the startup test in T043 must pass. Complete `ListTasksValidator` (injecting `IOptions<TaskListOptions>`) and `ListTasksHandler` parsing in `src/TaskLane.Application/Tasks/ListTasks/` until T041 passes.
- [ ] T045 [US4] Complete `TaskRepository.ListAsync` in `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs` until T042 passes:
  - Search uses `EF.Functions.Like` with an escape character over `Title` and `Description`.
  - Filters for status and range.
  - Sorting: `lower(Title)` for title, nulls-last ordering for the due date, and tie-breakers.
- [ ] T046 [US4] Add `GET /api/tasks/settings` (declared before `{id:guid}`) to `src/TaskLane.Api/Tasks/TasksController.cs`, returning `TaskSettingsResponse(int DueDateRangeMaxDays)` from `src/TaskLane.Api/Tasks/TaskSettingsResponse.cs`, until T043 passes.
- [ ] T047 [P] [US4] Write failing Vitest tests:
  - **`frontend/src/features/tasks/task-query.test.ts`:**
    - parsing from `URLSearchParams` and serializing back omits defaults;
    - invalid values (unknown status or sort, page `0`, `abc`, an inverted or one-sided range) are dropped and replaced with the defaults;
    - changing search or a filter resets the page to 1;
    - the default sort differs between active and deleted mode;
    - each sort field has its default direction (`dueDate` asc, `title` asc, `createdAt` desc, `deletedAt` desc), and a direction equal to that default is omitted from the URL.
  - **`frontend/src/features/tasks/TaskFilters.test.tsx`:**
    - labelled search, status select, "Due from" and "Due to" with the hint "Up to {N} days." from the settings endpoint;
    - a range longer than N, an inverted range or a one-sided range shows the API's message and does not change the URL;
    - sort field and direction selects;
    - "Clear search and filters".
  - **`frontend/src/features/tasks/TaskListPage.test.tsx`:**
    - the list reflects the URL (query string sent to the API);
    - typing a search updates the URL after the debounce (fake timers);
    - the empty state "No tasks match your search." has a clear action;
    - the same tools appear in deleted mode.
- [ ] T048 [US4] Implement the US4 SPA until T047 passes:
  - **Tasks feature:** `frontend/src/features/tasks/task-query.ts` and `TaskFilters.tsx`.
  - **API client:** `getTaskSettings` and query serialization in `tasks-api.ts`.
  - **Wiring:** use `TaskFilters` with URL state through `useSearchParams` in `TaskListPage.tsx`, and add a "no match" empty state.

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T049 [P] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Tasks/DemoTasksSeederTests.cs`:
  - Seeding with an existing demo account and no tasks creates six tasks for it: all three statuses, one overdue `ToDo` (today − 3, from `FakeTimeProvider`), one without a due date, one `Done` with a past date.
  - Seeding again creates nothing.
  - After all demo tasks are soft-deleted, seeding creates nothing.
  - An edited demo task is not modified by seeding.
  - With no demo account or no `DemoAccount` configuration, nothing happens.
- [ ] T050 Implement `DemoTasksSeeder` in `src/TaskLane.Infrastructure/Tasks/DemoTasksSeeder.cs` with a `SeedDemoTasksAsync()` extension, and call it after `SeedDemoAccountAsync()` in the Development block of `src/TaskLane.Api/Program.cs`. T049 must then pass.
- [ ] T051 [P] Update `docs/architecture.md` (Tasks module table, module-boundary rule, list-query flow), `README.md` (tasks, demo tasks, and the `Tasks__DueDateRangeMaxDays` setting) and the "Where things go" examples in `CLAUDE.md` if needed.
- [ ] T052 [P] Add sample requests for the eight task endpoints to `src/TaskLane.Api/TaskLane.Api.http`.
- [ ] T053 Run the automated gates from `specs/002-task-management/quickstart.md` §1:
  - `dotnet build` (0 warnings).
  - `dotnet format --verify-no-changes`.
  - `./scripts/test-coverage.ps1` (≥ 90 % Domain + Application, ≥ 80 % overall).
  - `npm run check`.

  Fix any failure.
- [ ] T054 Run the manual API checks from `specs/002-task-management/quickstart.md` §2 (including the restart with `Tasks__DueDateRangeMaxDays=30` and the demo-task idempotency check) and record the results.
- [ ] T055 Run the manual SPA check from `specs/002-task-management/quickstart.md` §3 (360 px width, keyboard only, clean console). The user performs the browser part, and the results, including the approximate time to create a task and see it in the list (SC-001), are recorded in the PR description.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup; blocks all user stories.
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**: depends on Foundational. It extends the US1 port, controller and `tasks-api.ts`, so in practice it runs after US1.
- **US3 (Phase 5)**: depends on US2 (`GetOwnedAsync`, `UpdateAsync` and the detail page).
- **US4 (Phase 6)**: depends on US1 (`ListAsync`, `TaskListPage`). The deleted-mode parts depend on US3.
- **Polish (Phase 7)**: T049 and T050 need US1 (`ITaskRepository.AddAsync`); the remaining tasks run after all stories.

### Within Each Story

- Tests (Red) → implementation (Green) → refactor, committed separately.
- Domain → Application slice → Infrastructure adapter → Api controller → SPA.

### Parallel Opportunities

- **Setup:** T002 alongside T001.
- **Foundational:** T003, T005, T007, T009 and T010 touch different files.
- **US1:** T012–T016 (tests) in parallel; T017 in parallel with test writing; T022 once the contract is fixed.
- **US2:** T024–T026 in parallel; T027 in parallel with T028; the frontend (T030/T031) in parallel with the backend.
- **US3:** T032–T035 in parallel; T039 in parallel with the backend.
- **US4:** T041–T043 in parallel; the frontend (T047/T048) in parallel with T044–T046.
- **Polish:** T049, T051 and T052 in parallel.

## Parallel Example: User Story 4

```text
# Red, in parallel:
T041 ListTasks validator rules (with configurable maximum)
T042 Repository search/filter/sort/paging + 500-task timing
T043 API query parameters + settings endpoint
T047 SPA task-query, TaskFilters, list URL state

# Green:
T044 → T045 → T046 (backend), T048 (frontend) in parallel
```

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 and Phase 2.
2. Phase 3 (US1). **Stop and validate**: create and list tasks per owner.

### Incremental Delivery

1. US1 → create and list (MVP).
2. US2 → view and edit. Together with US1 this is full CRUD minus delete.
3. US3 → soft delete and restore, which completes CRUD.
4. US4 → search, filters, sort and paging, plus the configurable range.
5. Polish → demo tasks, docs and gates, then a PR to `main`.

## Notes

- Every Green task names the test task it must make pass. Never weaken a test to reach green.
- Ownership is enforced inside repository queries; no handler loads a task and checks the owner afterwards.
- Migrations, configuration and docs are exempt from test-first (constitution II).
