# Contract: SPA routes and behavior for tasks

All routes below are protected by `RequireAuth` (feature 001).

## Routes

| Path | Screen |
|---|---|
| `/tasks` | Active task list with search, filters, sort, pagination; "New task" action |
| `/tasks/deleted` | Deleted task list (same tools, default sort: most recently deleted; shows deletion date) |
| `/tasks/new` | Create form |
| `/tasks/:id` | Task detail. Active task: actions "Edit" and "Delete". Deleted task: read-only, marked "Deleted on …", only action "Restore" |
| `/tasks/:id/edit` | Edit form (if the task turns out to be deleted, the screen redirects to its read-only detail) |

The account navigation gains a "Tasks" link when signed in.

## URL state (FR-019)

The list screens read and write their state from the query string, using the same names as the
API: `search`, `status`, `dueFrom`, `dueTo`, `sort`, `direction`, `page` and `pageSize`.
- Defaults are omitted from the URL.
- Invalid or unknown values are dropped and replaced with the defaults, without an error page.
- Changing search or any filter resets `page` to 1.
- The search input waits until the person stops typing for 300 ms before it updates the URL.

## Behaviors

| Trigger | Result |
|---|---|
| Create succeeds | Navigate to `/tasks/:id` with the status message "Task created." |
| Edit succeeds | Navigate to `/tasks/:id` with the status message "Task updated." |
| 400 on create or edit | Each `errors` entry is shown on its field; the values are kept |
| 409 on edit | Navigate to the read-only detail with the alert "This task was deleted. Restore it to edit." |
| 404 on detail or edit | The screen shows "Task not found" with a link back to `/tasks` (the same for missing and foreign tasks) |
| Delete | Confirmation dialog ("Delete this task? You can restore it from Deleted tasks.") with "Delete" and "Cancel"; on confirm → `/tasks` with the status message "Task deleted." |
| Restore | Back to `/tasks/:id`, now active, with the status message "Task restored." |
| Network error or 5xx | "We couldn't reach the service. Please try again." The form keeps its values |
| 401 | Handled by the session lifecycle from feature 001 |

## List rows

Each row shows:
- the title, linked to the detail;
- a status badge (To do, In progress or Done);
- the due date formatted in the local format, or "No due date";
- an **"Overdue"** badge when `status ≠ Done` and `dueDate < local today` (FR-012);
- in the deleted list only, the deletion date.

## Due-date range filter

- Two date inputs, "Due from" and "Due to", plus an "Apply" action.
- The maximum span comes from `GET /api/tasks/settings`; the hint reads "Up to {N} days."
- Client-side checks use the same messages as the API: both dates required, start ≤ end, span ≤ N.
  An invalid range is not applied to the URL, and the message is shown next to the inputs.

## Empty states (FR-022)

- No tasks at all (no filters active): "You have no tasks yet." with a "Create your first task"
  link. In the deleted list: "No deleted tasks."
- Filters active and nothing matches: "No tasks match your search." with a "Clear search and
  filters" action.

## Accessibility (FR-020, FR-021)

- The list is a semantic list. Pagination is a `nav` with `aria-label="Pagination"`, and the
  current page has `aria-current="page"`.
- Every field is labelled, and errors use `aria-describedby`.
- The confirmation dialog traps focus and returns it to the "Delete" button when closed.
- Save, delete and restore actions are disabled with `aria-busy` while a request is pending.
