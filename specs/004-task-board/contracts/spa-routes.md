# Contract: SPA routes and behavior for the task board

Replaces the task screens of [spec 002 spa-routes.md](../../002-task-management/contracts/spa-routes.md)
as amended by [spec 003](../../003-permanent-task-deletion/contracts/spa-routes.md). All routes are
protected by `RequireAuth` (feature 001). The API contract is unchanged.

## Routes

| Route | Shows |
|---|---|
| `/tasks` | The board: header ("Tasks", "New task"), filter bar, three columns |
| `/tasks/new` | Board + panel with the create form; `?status=ToDo\|InProgress\|Done` presets the status (invalid → "To do") |
| `/tasks/:id` | Board + panel with the task details ("Edit", "Delete"); "Task not found" when missing or foreign |
| `/tasks/:id/edit` | Board + panel with the edit form; "Task not found" when missing or foreign |

Every board address carries the board query string (`search`, `dueFrom`, `dueTo`, `sort`, `direction`;
see [data-model.md](../data-model.md)). Links to cards, "New task", "+", "Edit", and the panel's close
action keep it.

## Board

- Header: `<h1>Tasks</h1>` and a primary "New task" link (`/tasks/new` + query string).
- Filter bar: `role="search"`, `aria-label="Find tasks"`; controls in order: Search, Due from, Due to,
  Apply, Sort by (Due date, Created, Title), Order (Ascending, Descending), "Clear search and filters"
  (only when a search or range is active).
- Columns, in order To do, In progress, Done. Each column is a `<section>` with a header containing:
  - a colored dot and the name (`<h2>`),
  - the count (for example "8 tasks", "1 task"),
  - a "+" link labelled "New task in <name>" (`/tasks/new?status=<status>` + query string).
- Cards: `<ul aria-label="<name>">` of `<li>`; each card has:
  - the title as a link to `/tasks/:id` + query string,
  - up to two lines of the description,
  - the due date ("Due Nov 20, 2026") or "No due date",
  - "Overdue" when it applies,
  - a "Delete" button with accessible name "Delete <title>".
- "Load more" button at the bottom of a column while it has more tasks; "Loading more…" and disabled
  while loading.
- Status messages (`role="status"` / `role="alert"`) are shown above the board, as before.

## Empty and error states

| Situation | Shows |
|---|---|
| Loading a column | "Loading tasks…" in that column |
| A column with no tasks (others have some) | "No tasks" in that column |
| No tasks at all and no search or range | Instead of the columns' empty messages: "You have no tasks yet." with "Create your first task" (`/tasks/new`) |
| Search or range active and no column has matches | "No tasks match your search." with "Clear search and filters" |
| A column fails to load (`400` with a field error) | The API message (for example "Choose a range of at most 90 days.") |
| A column fails to load (other) | "We couldn't reach the service. Please try again." in that column |

## Panel

- A modal `<dialog>` at the right edge (`sm` and up: width about `32rem`, full height); full screen
  below `sm`. Board visible behind a light backdrop.
- `aria-labelledby` the content heading: the task title, "New task" or "Edit task".
- A close button labelled "Close" at the top right. Close, Escape and browser Back go to `/tasks` +
  board query string; focus returns to the element that opened the panel when it is still on the page.
  - Close and Escape replace the panel's address with `/tasks` + board query string; saving, cancelling
    and deleting from the panel also replace the address. Back afterwards never reopens the panel or
    returns to a submitted form.
  - The board query string is rebuilt from the board values only; the create form's `status` preset
    never reaches the board address.
- Content:
  - **Details**: status badge, overdue marker, due date, description (full), created, last updated;
    actions "Edit" (`/tasks/:id/edit` + query string) and "Delete".
  - **Create**: the task form ("Create task" / "Creating…"); on success go to `/tasks/:id` + query string
    with "Task created.".
  - **Edit**: the task form ("Save changes" / "Saving…") and "Cancel" (back to `/tasks/:id`); on success
    go to `/tasks/:id` + query string with "Task updated."; a `404` goes to `/tasks` with the alert
    "This task no longer exists." (spec 003).

## Deleting

The spec 003 confirmation ("Delete this task permanently? This can't be undone."), from the card or
the panel. On success: `/tasks` + query string with "Task deleted." (the panel closes). On `404`:
`/tasks` + query string with the alert "This task no longer exists.". Other failures: the service
message, the task stays.

## Accessibility

- Every control is labelled; focus is visible; everything is reachable by keyboard.
- The panel and the confirmation are modal dialogs: focus moves in and is trapped; Escape closes.
- Save, delete and "Load more" are disabled with `aria-busy` while pending.
