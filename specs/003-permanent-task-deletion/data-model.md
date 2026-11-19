# Data Model: Permanent Task Deletion

Changes relative to [spec 002 data-model](../002-task-management/data-model.md). Everything not
listed here is unchanged.

## TaskItem (Domain)

| Member | Change |
|---|---|
| `DeletedAt` (`DateTimeOffset?`) | **Removed** |
| `IsDeleted` | **Removed** |
| `Delete(now)` | **Removed**: deletion is a persistence operation, no state change on the entity |
| `Restore()` | **Removed** |
| `Update(...)` | No longer checks for the deleted state |

Lifecycle: a task **exists** (created → updated any number of times) or **does not exist**. There is
no intermediate state.

## Tasks table (SQLite)

| Column / index | Change |
|---|---|
| `DeletedAt` (INTEGER, UTC ticks, nullable) | **Dropped** |
| `IX_Tasks_OwnerId_DeletedAt_DueDate` | **Dropped**, replaced by `IX_Tasks_OwnerId_DueDate` (`OwnerId`, `DueDate`) |
| `IX_Tasks_OwnerId_DeletedAt_CreatedAt` | **Dropped**, replaced by `IX_Tasks_OwnerId_CreatedAt` (`OwnerId`, `CreatedAt`) |

Data migration (`RemoveTaskSoftDelete`, research R5): rows with `DeletedAt IS NOT NULL` are deleted
before the column is dropped. Rows without it are kept unchanged.

## Application types

| Type | Change |
|---|---|
| `ITaskRepository.DeleteOwnedAsync(id, ownerId)` → `bool` | **New**: removes the owner's task; `false` when no row matched |
| `ITaskRepository.UpdateAsync(task)` → `bool` | **Changed** from `Task`: `false` when the row no longer exists (research R3) |
| `ITaskRepository.GetOwnedAsync` | Doc only: there are no deleted tasks any more |
| `TaskListQuery.Deleted` | **Removed** |
| `TaskSortField.DeletedAt` | **Removed** |
| `ListTasksQuery.Deleted` | **Removed** |
| `TaskResponse.DeletedAt` | **Removed** |
| `TaskErrors.Deleted` | **Removed** |
| `DeleteTaskHandler` | Returns `Result<Guid>`; `NotFound` when nothing was deleted |
| `RestoreTask` slice (command, handler, DI registration) | **Removed** |

## Validation rules

Unchanged, except the list `sort` parameter, which no longer accepts `deletedAt` (only `dueDate`,
`createdAt`, `title`).
