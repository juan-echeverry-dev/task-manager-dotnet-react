# 0011. Permanent task deletion

- Status: Accepted
- Date: 2026-11-19

## Context

Spec 002 made deletion recoverable: a deleted task kept a `DeletedAt` timestamp, left the task list,
appeared in a separate "Deleted tasks" list, could be viewed read-only, and could be restored. The
repository owner decided that deleting a task should remove it for good, and that the product should
offer a single task list (the upcoming kanban board, spec 004, has no place for a recovery area).
[Spec 003](../../specs/003-permanent-task-deletion/spec.md) amends spec 002 accordingly.

## Decision

- Deleting a task removes its row with one owner-scoped statement
  (`ExecuteDeleteAsync` on `Id` and `OwnerId`). A missing, foreign or already deleted task answers
  `404 Task not found`, so ownership stays invisible.
- An edit whose task disappears between loading and saving (EF Core reports the zero-row `UPDATE`
  as a concurrency failure) also answers `404` instead of a server error.
- The deleted-tasks list, restore, the `409 Task is deleted` outcome and the `deletedAt` property are
  removed from the API and the SPA. `TaskItem` no longer has a deleted state.
- The migration `RemoveTaskSoftDelete` first deletes the rows already soft-deleted, then drops the
  `DeletedAt` column and replaces its indexes with `(OwnerId, DueDate)` and `(OwnerId, CreatedAt)`.

## Alternatives considered

- **Keep soft delete** (spec 002): recoverable, but it keeps a second list, a read-only view and a
  conflict outcome that the product no longer wants.
- **Undo period or recycle bin with automatic purge**: safer against mistakes, but adds a background
  job and state for little value in a single-user task list; the confirmation dialog states that
  deletion is permanent.
- **Load the task, then remove it**: two round trips, and a concurrent delete between them needs the
  same concurrency handling anyway.

## Consequences

- Deleted tasks cannot be recovered; the confirmation dialog is the only safeguard.
- `DELETE /api/tasks/{id}` is no longer idempotent in its status code: a repeated call answers `404`.
  The resulting state is the same, so retrying is still safe.
- In local development, an account whose demo tasks were all deleted is seeded again on the next
  start, because the seeder runs when the account has no tasks.
- On SQLite, dropping the column makes EF Core rebuild the `Tasks` table with
  `PRAGMA foreign_keys = 0`, which cannot run inside a transaction. If the process stops during that
  step, the migration must be repaired manually. The table is small and the step takes milliseconds.
- `Down` restores the column and indexes, not the removed rows.
