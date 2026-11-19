# Research: Permanent Task Deletion

No `NEEDS CLARIFICATION` items remained after the spec. The decisions below settle how the change
is made in the existing code base (feature 002).

## R1. How a task is deleted

- **Decision**: Delete with a single owner-scoped statement:
  `ExecuteDeleteAsync` on `Tasks WHERE Id = @id AND OwnerId = @owner`. The repository reports
  whether a row was removed; zero rows means "task not found".
- **Rationale**: One round trip; ownership stays in the query (spec 002 FR-002); two concurrent
  deletes are naturally safe: the second affects zero rows and reports "not found" (spec edge case).
- **Alternatives considered**:
  - Load with `GetOwnedAsync`, then `Remove` + `SaveChangesAsync`: two round trips, and a
    concurrent delete between load and save raises `DbUpdateConcurrencyException`, which then needs
    handling anyway.
  - Keep the domain `Delete` method with a flag: that is the soft delete being removed.

## R2. Result of the delete use case

- **Decision**: `DeleteTaskHandler` returns `Result<Guid>` (the deleted id). The controller still
  answers `204 No Content`.
- **Rationale**: `Result` is generic only (`Result<T>`); the task no longer exists after the
  statement, so there is no `TaskResponse` to return. Returning the id avoids adding a non-generic
  `Result` type for one use case (constitution V).
- **Alternatives considered**: a non-generic `Result`/`Unit` type (new abstraction for a single use);
  loading the task first to return it (extra query, see R1).

## R3. Editing a task deleted in the meantime

- **Decision**: `ITaskRepository.UpdateAsync` returns `bool`; it catches
  `DbUpdateConcurrencyException` (EF Core raises it when the `UPDATE` affects zero rows) and returns
  `false`. `UpdateTaskHandler` maps `false` to `TaskErrors.NotFound`.
- **Rationale**: With soft delete the row never disappeared; with permanent deletion a task can vanish
  between load and save. Spec FR-003 requires "task not found", not a server error.
- **Alternatives considered**: ignoring the race (would surface as `500`); a concurrency token column
  (unneeded: zero affected rows already signals the case).

## R4. Removing the soft-delete state

- **Decision**: Remove `DeletedAt`, `IsDeleted`, `Delete()` and `Restore()` from `TaskItem`; remove the
  `RestoreTask` slice, `TaskErrors.Deleted`, `TaskSortField.DeletedAt`, `TaskListQuery.Deleted`,
  `ListTasksQuery.Deleted`, the deleted-list branch of `ListSorting`, `TaskResponse.DeletedAt`, and the
  `GET /api/tasks/deleted` and `POST /api/tasks/{id}/restore` actions.
- **Rationale**: Spec FR-005; no dead code or speculative leftovers (constitution I and V).
- **Alternatives considered**: keeping `deletedAt: null` in the response for compatibility: the only
  client is the SPA, changed in the same feature.

## R5. Migration and data cleanup

- **Decision**: One migration, `RemoveTaskSoftDelete`:
  1. `DELETE FROM "Tasks" WHERE "DeletedAt" IS NOT NULL;` (spec FR-006),
  2. drop indexes `IX_Tasks_OwnerId_DeletedAt_DueDate` and `IX_Tasks_OwnerId_DeletedAt_CreatedAt`,
  3. drop column `DeletedAt` (EF Core rebuilds the SQLite table),
  4. create `IX_Tasks_OwnerId_DueDate` and `IX_Tasks_OwnerId_CreatedAt`.

  `Down` re-adds the nullable column and the old indexes; the removed rows are not recoverable.
- **Rationale**: Cleanup and schema change happen atomically on startup, as migrations already run;
  indexes keep covering the default orders (spec 002 SC-004).
- **Alternatives considered**: a separate cleanup script (extra manual step; README must stay enough
  to run the app).

## R6. Demo seeding

- **Decision**: Keep `DemoTasksSeeder`'s rule (`AnyAsync` on the owner's tasks); update its comment and
  the first sample's description ("…creating, finding and restoring tasks" → "…creating, finding and
  deleting tasks").
- **Rationale**: Spec FR-008; the code already counts all rows, which are now only active tasks.

## R7. SPA changes

- **Decision**:
  - Remove `listDeletedTasks`, `restoreTask`, `Task.deletedAt`, the `deleted` mode of `TaskListPage`
    and `task-query.ts` (including the `deletedAt` sort), the `/tasks/deleted` route and link, the
    read-only deleted view and `RestoreAction` in `TaskDetailPage`, the deleted redirect and `409`
    handling in `EditTaskPage`, and the `restored` / `deletedRestoreToEdit` messages.
  - Confirmation text: "Delete this task permanently? This can't be undone."
  - A `404` while saving an edit or confirming a delete navigates to `/tasks` with the alert
    "This task no longer exists." (new message `noLongerExists`).
- **Rationale**: Spec FR-001, FR-003, FR-005.
- **Former `/tasks/deleted` address**: after the route is removed, it matches `/tasks/:id` with
  id `deleted`; the API answers `404` (the `{id:guid}` constraint does not match) and the SPA shows its
  "Task not found" screen with a link back. The spec was worded accordingly ("a not found screen").
  A dedicated route or a GUID check only for this address was rejected as code kept for a feature
  that no longer exists.

## R8. Documentation

- **Decision**: ADR `0011-permanent-task-deletion.md` (spec FR-009) and an "Amended by spec 003"
  banner at the top of `specs/002-task-management/spec.md`, `plan.md`, `data-model.md` and both
  contracts. Spec 002 content is otherwise left as the historical record.
- **Rationale**: ADRs are immutable and decisions change by adding records (docs/adr/README.md); the
  banner prevents readers from following outdated contracts.
