# 0012. Task board with a routed side panel

- Status: Accepted
- Date: 2026-11-19

## Context

Spec 002 presented tasks as a paginated list with separate pages for details, create and edit. The
repository owner asked for a kanban board with one column per status, where opening, creating and
editing a task does not leave the board ([spec 004](../../specs/004-task-board/spec.md)). The API of
specs 002/003 already lists tasks by one status with paging, and the spec rules out API changes.

## Decision

- **Board**: `/tasks` renders three columns (To do, In progress, Done). Each column is the existing
  `GET /api/tasks` filtered by its status, loaded 20 at a time with TanStack `useInfiniteQuery` and a
  "Load more" button. The board's search, due-date range and sort live in the query string.
- **Side panel as nested routes**: `/tasks/new`, `/tasks/:id` and `/tasks/:id/edit` are child routes of
  `/tasks`, rendered by a `TaskPanel` layout route inside the mounted board. Every panel state has its
  own address (reload and sharing work), the board keeps its loaded columns, and the board query string
  travels on every panel address.
- **Native modal `<dialog>`**: the panel uses `showModal()` like the existing confirmation dialog. The
  browser traps focus, makes the board inert and handles Escape; the panel is named by its content's
  heading and returns focus to the card that opened it.
- **Closing replaces the address**: Close, Escape, save, cancel and delete navigate with `replace`, so the
  browser's Back never reopens a closed panel or returns to a submitted form.

## Alternatives considered

- **Panel state in a query parameter** (`/tasks?task=…`): would change every existing task address.
- **Modal or drawer without an address**: no reload or sharing of an open task.
- **A drawer component library**: a new dependency for what the native `<dialog>` already provides.
- **A "board" endpoint returning the three columns at once**: one request instead of three, but a new
  contract and use case for no measurable gain at this scale.
- **Closing with history back when opened from the board**: after Edit → Save, Back would show the
  form again; replacing the address is simpler and predictable.

## Consequences

- A filter change sends three list requests (one per column); fine for the target scale (spec 002
  SC-004), and each column loads, fails and pages on its own.
- While the panel is open the board is inert (modal): the person closes the panel to act on another card.
- The status filter is gone from the screen (columns are the statuses); the API keeps supporting it.
- Closing adds no history entry, so the browser's Back after closing goes to the previous board state,
  not to the panel.
