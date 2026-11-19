# Contract delta: SPA routes and messages

Changes to [spec 002 spa-routes.md](../../002-task-management/contracts/spa-routes.md). Everything
not listed is unchanged.

## Routes

| Route | Change |
|---|---|
| `/tasks` | The "Deleted tasks" link is removed. Sort options: due date, created, title |
| `/tasks/deleted` | **Removed.** The address now matches `/tasks/:id` and shows "Task not found" with "Back to tasks" |
| `/tasks/:id` | Only existing tasks; actions "Edit" and "Delete". No read-only deleted view, no "Restore" |
| `/tasks/:id/edit` | No redirect for deleted tasks; a missing task shows "Task not found" |

## Outcomes and messages

| Situation | Before | After |
|---|---|---|
| Delete confirmation text | "Delete this task? You can restore it from Deleted tasks." | "Delete this task permanently? This can't be undone." |
| Confirm delete | `/tasks` with "Task deleted." | Unchanged |
| Delete answers `404` (already deleted elsewhere) | Error message | `/tasks` with the alert "This task no longer exists." (list refreshed) |
| Save edit answers `404` | "Task not found" screen | `/tasks` with the alert "This task no longer exists." |
| Save edit answers `409` | Read-only detail with "This task was deleted. Restore it to edit." | **Removed** (the API no longer returns `409`) |
| Restore | Back to detail with "Task restored." | **Removed** |

## Accessibility

Unchanged: the confirmation dialog traps focus and returns it to "Delete"; save and delete actions
are disabled with `aria-busy` while pending (spec 003 FR-007).
