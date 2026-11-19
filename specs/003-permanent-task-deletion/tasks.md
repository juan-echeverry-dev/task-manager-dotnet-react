---

description: "Task list for feature 003 — Permanent Task Deletion"
---

# Tasks: Permanent Task Deletion

**Input**: Design documents from `specs/003-permanent-task-deletion/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: REQUIRED. Constitution principle II (Test-First) applies:
- Each test task is written first and MUST fail (Red) before the implementation task that follows it
  (Green). An optional refactor closes the cycle.
- Commit each step separately (`test:`, `feat:`, `refactor:`), only after the repository owner
  confirms each change.
- Removing code is not a behavior and needs no failing test. The test that proves the removal (for
  example "the endpoint answers `404`") is still written first.
- Commits carry no co-author trailers.

**Organization**: The tasks follow the three user stories of the spec, and they come in this order
so that each story builds and passes on its own:
- **US1** makes deletion permanent; the `DeletedAt` column stays for now.
- **US2** removes recovery from the Application, API and SPA.
- **US3** removes the column and cleans up its data.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: User story from spec.md (US1–US3)
- Paths are relative to the repository root.
- Conventions from features 001 and 002:
  - Handlers return `Result<T>`, and controllers map them with `ResultMappings`.
  - Tests use `TestContext.Current.CancellationToken`.
  - Not found means `TaskErrors.NotFound` = `Error.NotFound("Task not found", "The task does not exist.")`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm a green baseline before changing behavior.

- [X] T001 Run `dotnet build TaskLane.slnx`, `dotnet test --solution TaskLane.slnx` and `cd frontend; npm run check` on branch `003-permanent-task-deletion` and record that all pass (baseline; no file changes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Repository port changes that US1 builds on.

- [X] T002 Write failing real-SQLite tests in `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs`:
  - `DeleteOwnedAsync` removes the owner's task (the row is gone) and returns `true`.
  - It returns `false` and changes nothing for a missing id, and for another owner's task.
  - A second call for the same id returns `false`.
  - `UpdateAsync` returns `true` for an existing tracked task.
  - `UpdateAsync` returns `false`, without throwing, when the row was removed by another context
    after `GetOwnedAsync` (research R3).
- [X] T003 In `src/TaskLane.Application/Tasks/Ports/ITaskRepository.cs`:
  - Add `Task<bool> DeleteOwnedAsync(Guid id, Guid ownerId, CancellationToken cancellationToken)`.
  - Change `UpdateAsync` to return `Task<bool>`.
  - Write their XML docs: "false when no row of this owner matched" and "false when the task no longer exists".
  - Change the `GetOwnedAsync` doc from "The owner's task, active or deleted, tracked for changes" to "The owner's task, tracked for changes".
- [X] T004 Implement both in `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs`:
  - `DeleteOwnedAsync` is `context.Tasks.Where(task => task.Id == id && task.OwnerId == ownerId).ExecuteDeleteAsync(cancellationToken)` and returns `> 0`.
  - `UpdateAsync` catches `DbUpdateConcurrencyException`, returns `false` and detaches the entry.
  - Adjust the existing callers so the solution builds (`UpdateTaskHandler` and `RestoreTaskHandler`
    may ignore the result until US1 and US2).
  - T002 now passes.

**Checkpoint**: Port ready; behavior unchanged for users.

---

## Phase 3: User Story 1 - Delete a task permanently (Priority: P1) 🎯 MVP

**Goal**: Confirming "Delete" removes the task for good. Afterwards, opening, editing or deleting it
answers "task not found".

**Independent Test**:
1. Create a task, then `DELETE` it: `204`.
2. `GET`, `PUT` and `DELETE` on it now answer `404`.
3. It is gone from `GET /api/tasks` and its count.
4. In the SPA, the dialog says the deletion is permanent.

### Tests for User Story 1 (write first, must fail)

- [X] T005 [P] [US1] Rewrite `tests/TaskLane.UnitTests/Tasks/DeleteTask/DeleteTaskHandlerTests.cs` with NSubstitute:
  - It calls `DeleteOwnedAsync(taskId, ownerId)` and returns `Result<Guid>` success with the task id.
  - It returns `TaskErrors.NotFound` when the call returns `false`.
  - It no longer calls `GetOwnedAsync` or `UpdateAsync`.
- [X] T006 [P] [US1] Add a test to `tests/TaskLane.UnitTests/Tasks/UpdateTask/UpdateTaskHandlerTests.cs`: when `UpdateAsync` returns `false`, the handler returns `TaskErrors.NotFound`
- [X] T007 [P] [US1] Create `tests/TaskLane.Api.Tests/Tasks/DeleteTaskTests.cs` with these cases (spec US1 scenarios 2, 4 and 5, edge cases; contracts/tasks-api.md):
  - `DELETE` answers `204`, and the task is then absent from `GET /api/tasks` and from `totalCount`.
  - `GET` of a deleted task answers `404`.
  - `PUT` of a deleted task answers `404` with title "Task not found" (not `409`).
  - A second `DELETE` answers `404`.
  - `DELETE` of another person's task answers `404`, and that task still exists for its owner.
  - `DELETE` without a session answers `401`.
- [X] T008 [P] [US1] Add tests to `frontend/src/features/tasks/TaskDetailPage.test.tsx`:
  - The dialog shows "Delete this task permanently? This can't be undone."
  - A `404` on confirm navigates to `/tasks` with the alert "This task no longer exists."
  - Keep these existing tests, and they must still pass:
    - "changes nothing when cancelled" (FR-004).
    - "disables the confirm action while deleting" (FR-007).
    - "shows the service message when deleting fails" (edge case: service unreachable).
    - In `TaskListPage.test.tsx`, "shows an empty state with a link to create the first task" (deleting the last task).
  - Add one test to `TaskListPage.test.tsx`: a page beyond the last (`items: []`, `totalCount > 0`) shows "This page is empty." (edge case: deleting the last task of a page).
- [X] T009 [P] [US1] Add a test to `frontend/src/features/tasks/EditTaskPage.test.tsx`: a `404` on save navigates to `/tasks` with the alert "This task no longer exists."

### Implementation for User Story 1

- [X] T010 [US1] Rewrite `src/TaskLane.Application/Tasks/DeleteTask/DeleteTaskHandler.cs`:
  - Constructor `(ITaskRepository tasks)`, without `TimeProvider`.
  - It returns `Result<Guid>`: `NotFound` when `DeleteOwnedAsync` returns `false`.
  - Update the XML doc to say "permanently removes (spec 003 FR-001 to FR-003)".
  - T005 passes.
- [X] T011 [US1] In `src/TaskLane.Application/Tasks/UpdateTask/UpdateTaskHandler.cs`, map `UpdateAsync` returning `false` to `TaskErrors.NotFound` (T006 passes)
- [X] T012 [US1] Adapt `Delete` in `src/TaskLane.Api/Tasks/TasksController.cs` to the `Result<Guid>` handler (still `204 No Content`, `404` documented); remove the old soft-delete cases from `tests/TaskLane.Api.Tests/Tasks/DeleteAndRestoreTaskTests.cs` that T007 replaces (T007 passes)
- [X] T013 [P] [US1] In `frontend/src/features/tasks/messages.ts`:
  - Set `deleteConfirmation` to "Delete this task permanently? This can't be undone."
  - Add `noLongerExists: 'This task no longer exists.'`.
- [X] T014 [US1] In `DeleteAction` of `frontend/src/features/tasks/TaskDetailPage.tsx`, on `ApiError` status `404`: invalidate `['tasks']` and navigate to `/tasks` with `{ alert: taskMessages.noLongerExists }`. Any other error keeps the current behavior (T008 passes)
- [X] T015 [US1] In `frontend/src/features/tasks/EditTaskPage.tsx`, on a `404` save error: invalidate `['tasks']` and navigate to `/tasks` with `{ alert: taskMessages.noLongerExists }` (T009 passes)

**Checkpoint**: Deletion is permanent end to end; the restore screens still exist but no new task can
reach them.

---

## Phase 4: User Story 2 - No trace of the former deleted-tasks area (Priority: P2)

**Goal**: No deleted-tasks list, no "Restore", and no read-only deleted view in the API or the SPA.

**Independent Test**:
1. `GET /api/tasks/deleted` and `POST /api/tasks/{id}/restore` answer `404`.
2. `GET /api/tasks?sort=deletedAt` answers `400`.
3. The task response has no `deletedAt`.
4. The SPA has no "Deleted tasks" link and no "Restore", and `/tasks/deleted` shows "Task not found".

### Tests for User Story 2 (write first, must fail)

- [ ] T016 [P] [US2] Create `tests/TaskLane.Api.Tests/Tasks/RemovedEndpointsTests.cs` (spec US2 scenario 4):
  - `GET /api/tasks/deleted` answers `404`, and so does `POST /api/tasks/{id}/restore` on an existing task.
  - `GET /api/tasks?sort=deletedAt` answers `400` with "Sort must be one of: dueDate, createdAt, title.".
  - The JSON of `GET /api/tasks/{id}` has no `deletedAt` property.
- [ ] T017 [P] [US2] Update `tests/TaskLane.UnitTests/Tasks/ListTasks/ListTasksValidatorTests.cs` and `ListTasksHandlerTests.cs`: `deletedAt` is rejected as a sort field, and the queries have no `Deleted` flag
- [ ] T018 [P] [US2] Update the frontend tests:
  - `frontend/src/features/tasks/TaskListPage.test.tsx`: no "Deleted tasks" link; the sort options are exactly Due date, Created and Title.
  - `frontend/src/app/routes.test.tsx`: `/tasks/deleted` shows "Task not found" with "Back to tasks".
  - `frontend/src/features/tasks/TaskDetailPage.test.tsx`: no "Restore" button.
  - `frontend/src/features/tasks/task-query.test.ts`: no mode argument, and `deletedAt` is not a sort field.

### Implementation for User Story 2

- [ ] T019 [US2] Delete `src/TaskLane.Application/Tasks/RestoreTask/` and `tests/TaskLane.UnitTests/Tasks/RestoreTask/`; remove `RestoreTaskHandler` registration from `src/TaskLane.Application/DependencyInjection.cs`
- [ ] T020 [US2] Remove `TaskErrors.Deleted` from `src/TaskLane.Application/Tasks/TaskErrors.cs` and the `IsDeleted` check from `src/TaskLane.Application/Tasks/UpdateTask/UpdateTaskHandler.cs` (and its unit test)
- [ ] T021 [US2] Remove the deleted list from Application:
  - `src/TaskLane.Application/Tasks/Ports/TaskListQuery.cs`: remove `Deleted` and `TaskSortField.DeletedAt`.
  - `ListTasks/ListTasksQuery.cs`: remove `Deleted`.
  - `ListTasks/ListSorting.cs`: one field set (`dueDate`, `createdAt`, `title`), default `DueDate`; only `CreatedAt` defaults to descending.
  - `ListTasks/ListTasksValidator.cs` and `ListTasks/ListTasksHandler.cs`: drop the `deleted` parameter.
  - Update the XML docs (T017 passes).
- [ ] T022 [US2] Remove `DeletedAt` from `src/TaskLane.Application/Tasks/TaskResponse.cs`; update `GetTask/GetTaskHandler.cs` doc ("existing task")
- [ ] T023 [US2] In `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs`, remove the `query.Deleted` branch (keep `DeletedAt == null` until US3) and the `TaskSortField.DeletedAt` ordering case. In the same change, so the solution compiles after T021, **remove** (do not rewrite) the deleted-list tests:
  - In `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryQueryTests.cs`: `SortByDeletedAt_FollowsTheDirection` and the deleted-list filter test, the one with `Deleted = true` near line 197.
  - In `tests/TaskLane.Infrastructure.Tests/Tasks/TaskRepositoryTests.cs`: the deleted-list query field (line 20) and every test that lists deleted tasks or calls `Delete` (around lines 52, 142–144, 157 and 174).
  - Ownership and "not found" cases among them are already covered by T002 (`DeleteOwnedAsync`).
- [ ] T024 [US2] In `src/TaskLane.Api/Tasks/TasksController.cs`:
  - Remove `ListDeleted`, `Restore`, the `409` `ProducesResponseType` on `Update`, and the `deleted` parameter of `ListAsync`.
  - Delete `tests/TaskLane.Api.Tests/Tasks/DeleteAndRestoreTaskTests.cs` (fully replaced by T007 and T016).
  - Fix `FindTasksTests.cs` and `CreateAndListTasksTests.cs` (T016 passes).
- [ ] T025 [US2] Remove `Delete(now)`, `Restore()` and `IsDeleted` from `src/TaskLane.Domain/Tasks/TaskItem.cs`, and the `Update` deleted guard. Remove the tests that used them:
  - `tests/TaskLane.UnitTests/Tasks/TaskItemTests.cs`: the delete, restore and "cannot edit when deleted" cases.
  - `tests/TaskLane.Infrastructure.Tests/Tasks/TaskPersistenceTests.cs`: the `DeletedAt` round-trip (around lines 23–41) and the ordering by `DeletedAt` (around lines 84–94).
  - `tests/TaskLane.Infrastructure.Tests/Tasks/DemoTasksSeederTests.cs`: the test that does not re-seed when the account has only deleted tasks (around lines 72–82). The behavior changes on purpose (spec 003, Assumptions: demo tasks come back after deleting them all), so replace it with a test asserting that an account whose tasks were all removed is seeded again.

  Soft-deleted rows are no longer built through the domain anywhere. Only T031 creates one, by raw SQL.
- [ ] T026 [P] [US2] In `frontend/src/features/tasks/tasks-api.ts`, remove `deletedAt` from `Task`, and remove `listDeletedTasks` and `restoreTask`; update `tasks-api.test.ts` and `test-data.ts`
- [ ] T027 [US2] In `frontend/src/features/tasks/task-query.ts`, remove `TaskListMode` and the `deletedAt` sort, and drop the mode parameters from `sortFields`, `defaultTaskQuery`, `parseTaskQuery`, `serializeTaskQuery` and `toApiParams`
- [ ] T028 [US2] Simplify `frontend/src/features/tasks/TaskListPage.tsx` and `TaskFilters.tsx`:
  - Use a single mode: remove `MODES`, the `mode` prop, the "Deleted tasks" and "Back to tasks" links, and the `deletedAt` display in `TaskRow`.
  - Use the query key `['tasks', 'list', params, accessToken]`.
- [ ] T029 [US2] In `frontend/src/features/tasks/TaskDetailPage.tsx`, remove `RestoreAction`, the deleted branch and the "Deleted on" line. In `EditTaskPage.tsx`, remove the `deletedAt` redirect and the `409` handling. In `messages.ts`, remove `restored` and `deletedRestoreToEdit`
- [ ] T030 [US2] Remove the `tasks/deleted` route from `frontend/src/app/routes.tsx` (T018 passes); run `cd frontend; npm run check`

**Checkpoint**: Nothing in the product refers to deleted tasks; the column still exists but is unused
except as a filter.

---

## Phase 5: User Story 3 - Existing deleted tasks are cleaned up (Priority: P3)

**Goal**: Rolling out removes the rows already soft-deleted and the `DeletedAt` column, and leaves
active tasks unchanged.

**Independent Test**:
1. Start from a database migrated to `AddTasks`, with one active and one soft-deleted row.
2. Apply `RemoveTaskSoftDelete`.
3. Only the active row remains, with identical values, and the column is gone.

### Tests for User Story 3 (write first, must fail)

- [ ] T031 [US3] Create `tests/TaskLane.Infrastructure.Tests/Persistence/RemoveTaskSoftDeleteMigrationTests.cs` on a real SQLite connection (reuse `Support/SqliteDatabase.cs`):
  1. Migrate to `AddTasks` with `IMigrator`.
  2. Insert a user plus one active and one soft-deleted task by raw SQL.
  3. Migrate to latest.
  4. Assert that only the active task remains, with unchanged title, description, status, due date,
     `CreatedAt` and `UpdatedAt`.
  5. Assert that `PRAGMA table_info("Tasks")` has no `DeletedAt` column.
  6. Assert that the indexes `IX_Tasks_OwnerId_DueDate` and `IX_Tasks_OwnerId_CreatedAt` exist.

### Implementation for User Story 3

- [ ] T032 [US3] Remove `DeletedAt` from `src/TaskLane.Domain/Tasks/TaskItem.cs`. In `src/TaskLane.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`:
  - Remove the `DeletedAt` property and the `IsDeleted` ignore.
  - Replace the indexes with `HasIndex(task => new { task.OwnerId, task.DueDate }).HasDatabaseName("IX_Tasks_OwnerId_DueDate")` and `HasIndex(task => new { task.OwnerId, task.CreatedAt }).HasDatabaseName("IX_Tasks_OwnerId_CreatedAt")`.
  - Remove the `DeletedAt == null` filter from `src/TaskLane.Infrastructure/Tasks/TaskRepository.cs`.
  - Remove the `task.DeletedAt == null` assertion from `tests/TaskLane.Infrastructure.Tests/Tasks/DemoTasksSeederTests.cs` (around line 42).
- [ ] T033 [US3] Generate the migration with `dotnet ef migrations add RemoveTaskSoftDelete --project src/TaskLane.Infrastructure --startup-project src/TaskLane.Api --output-dir Persistence/Migrations`. Then:
  - Insert `migrationBuilder.Sql("DELETE FROM \"Tasks\" WHERE \"DeletedAt\" IS NOT NULL;");` as the first statement of `Up`.
  - Verify that `Down` restores the nullable column and the old indexes (T031 passes).
- [ ] T034 [P] [US3] In `src/TaskLane.Infrastructure/Tasks/DemoTasksSeeder.cs`:
  - Update the class doc so it no longer mentions deleted tasks.
  - Change the first sample's description to "Walk through creating, finding and deleting tasks.".
  - Confirm that `tests/TaskLane.Infrastructure.Tests/Tasks/DemoTasksSeederTests.cs` still passes (FR-008), adjusting only the references to deleted rows.

**Checkpoint**: Schema and data match spec 003; all three stories are complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T035 [P] Write `docs/adr/0011-permanent-task-deletion.md`:
  - Status Accepted, date 2026-11-19.
  - Context: the soft delete of spec 002.
  - Decision: permanent deletion, with the cleanup migration.
  - Alternatives: keep the soft delete; add an undo period.
  - Consequences: no recovery, and `DELETE` answers `404` on a repeat.
  - Add the row to `docs/adr/README.md`.
- [ ] T036 [P] Add an "Amended by [spec 003](../003-permanent-task-deletion/spec.md): deletion is permanent; deleted-tasks list and restore removed." banner below the title of `specs/002-task-management/spec.md`, `plan.md`, `data-model.md`, `contracts/tasks-api.md` and `contracts/spa-routes.md`
- [ ] T037 [P] Search `README.md`, `docs/` and `frontend/src` for "restore", "Deleted tasks" and "deletedAt", and update any leftover mention outside `specs/002-*` and migrations
- [ ] T038 Run the full gate:
  - `dotnet build TaskLane.slnx` (0 warnings).
  - `dotnet format TaskLane.slnx --verify-no-changes`.
  - `./scripts/test-coverage.ps1` (≥ 90 % Domain + Application, ≥ 80 % overall).
  - `cd frontend; npm run check`.
- [ ] T039 Run the manual walkthrough and the migration check in `specs/003-permanent-task-deletion/quickstart.md` and report the results to the repository owner

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)** → **Foundational (T002–T004)** → **US1** → **US2** → **US3** → **Polish**.
- These stories are sequential by design: each one removes what the previous one stopped using.
  - US1 depends on the Foundational port.
  - US2 removes the code that US1 left unused (`TaskItem.Delete`, `DeleteAndRestoreTaskTests`).
  - US3 removes the column that US2 left as a filter only.

### Within Each Story

- Tests first (Red), then implementation (Green).
- In the backend: Application → Infrastructure → Api. The frontend can go in parallel with the
  backend once the tests are written.

### Parallel Opportunities

- US1: T005, T006, T007, T008 and T009 can run in parallel (different files). T013 can run in
  parallel with the backend work T010–T012.
- US2: T016, T017 and T018 can run in parallel. The frontend tasks T026–T030 can run in parallel
  with the backend tasks T019–T025.
- Polish: T035, T036 and T037 can run in parallel.

## Parallel Example: User Story 1

```text
Together (tests, Red):
  T005 DeleteTaskHandlerTests      T006 UpdateTaskHandlerTests
  T007 DeleteTaskTests (API)        T008 TaskDetailPage.test.tsx
  T009 EditTaskPage.test.tsx
Then (Green): backend T010 → T011 → T012, in parallel with frontend T013 → T014 → T015
```

## Implementation Strategy

### MVP First (User Story 1)

Phases 1–3 alone deliver the user-visible change: deletion is permanent and safe under
concurrency. Stop and validate with the US1 independent test.

### Incremental Delivery

1. US1: permanent deletion (MVP).
2. US2: removes the recovery surface. Without it, the "Deleted tasks" screen would stay but always
   be empty for new deletions.
3. US3: data cleanup and schema. Without it, old soft-deleted rows would stay invisible but stored.
4. Polish: ADR, banners on spec 002, the full gate.

Each phase ends green (build, tests, `npm run check`) before the next starts. Each Red/Green step is
presented to the repository owner and committed only after confirmation.

## Notes

- Do not edit the existing migrations `AddUsers` or `AddTasks`.
- Spec 004 (kanban board) starts from `main` after this feature is merged.
