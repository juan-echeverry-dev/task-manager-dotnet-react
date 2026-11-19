# Research: Task Board

No `NEEDS CLARIFICATION` items remained after the spec. The decisions below settle how the board is
built in the existing SPA (ADR 0009: React, React Router, TanStack Query, Tailwind, no component
library).

## R1. No API change

- **Decision**: Each column is `GET /api/tasks` with `status=<column>` plus the board's search, range
  and sort, `pageSize=20`, and `page=n` for "Load more". The column count is the response's
  `totalCount`.
- **Rationale**: The existing endpoint already filters by one status and pages (spec 002 FR-016,
  FR-018). Three small requests per filter change stay well within SC-004 (500 tasks, < 1 s).
- **Alternatives considered**: a new "board" endpoint returning the three columns at once (one round
  trip, but a new contract and use case for no measurable gain; out of scope per the spec).

## R2. Columns load with `useInfiniteQuery`

- **Decision**: One TanStack `useInfiniteQuery` per column, wrapped in a `useBoardColumn(status, query)`
  hook that `TaskBoardPage` calls three times (so it knows the three counts for the board-level empty
  states) and passes to a presentational `BoardColumn`; key
  `['tasks', 'board', status, boardParams, accessToken]`, `getNextPageParam` returning `page + 1` while
  `page * pageSize < totalCount`. "Load more" calls `fetchNextPage`.
- **Rationale**: Built into the library already in use; each column loads, fails and pages on its own
  (spec edge cases); the existing `invalidateQueries({ queryKey: ['tasks'] })` after create, edit and
  delete keeps refreshing every column.
- **Alternatives considered**: one `useQuery` per page with manual concatenation (reimplements what
  `useInfiniteQuery` does); a larger single page (breaks FR-006).

## R3. Side panel as nested routes

- **Decision**: `/tasks` renders the board and an `<Outlet />`. Its child routes render the panel:
  `/tasks/new` (create, `?status=` presets the status), `/tasks/:id` (details), `/tasks/:id/edit` (edit).
  The board's search, range and sort stay in the query string on every child address, so opening and
  closing the panel keeps them, and links carry the current query string.
- **Rationale**: The board stays mounted while the panel opens and closes (no reload, keeps loaded
  batches); each panel state has its own address (FR-011); the browser's Back closes the panel
  naturally (FR-010); `/tasks/:id` and `/tasks/:id/edit` keep working as before.
- **Alternatives considered**: panel state in a query parameter (`?task=…`) on `/tasks` (changes every
  existing task link and breaks the spec 003 contract); a modal without an address (fails FR-011).

## R4. Panel shell on the native `<dialog>`

- **Decision**: A `TaskPanel` layout route renders a native `<dialog>` opened with `showModal()`,
  positioned at the right edge (full screen under the `sm` breakpoint), with a light backdrop so the
  board stays visible. It has a visible close button; the dialog's `cancel` event (Escape) and the close
  button share one action that replaces the address with `/tasks` and the board query string; saves,
  cancels and deletes in the panel also replace. Back never reopens a closed panel. (A `fromBoard` flag
  with `navigate(-1)` was considered and rejected: after Edit → Save, going back would show the form.) The content (detail, create, edit)
  receives the id for `aria-labelledby` through the outlet context and renders the heading.
  On close, focus returns to the element that opened it when it is still on the page, otherwise to the
  board heading.
- **Rationale**: Same approach as `ConfirmDialog` (spec 002): the browser traps focus and makes the board
  inert while the panel is open (FR-017), and the jsdom stand-in in `test/setup.ts` already supports it.
  A `ConfirmDialog` opened from inside the panel stacks on top (top layer).
- **Alternatives considered**: a non-modal `<aside>` (would need a hand-written focus trap); a
  third-party drawer component (new dependency, constitution V).

## R5. Address state

- **Decision**: Replace `task-query.ts` with `board-query.ts`: the board query is `search`, `dueFrom`,
  `dueTo`, `sort`, `direction`. `status`, `page` and `pageSize` in the address are ignored (spec edge
  case). `columnParams(query, status)` adds `status` and `pageSize=20` for the API; the infinite query
  adds `page`.
- **Rationale**: FR-016; the parsing, defaults and validation rules of spec 002 are kept (same
  functions, fewer fields).

## R6. Filter bar layout

- **Decision**: `TaskFilters` becomes one `flex flex-wrap items-end gap-3` row inside a bordered white
  bar: Search (grows), Due from, Due to, "Apply" (range), Sort by, Order, "Clear search and filters" (shown when any
  filter is active). The Status select is removed. Labels stay visible and small (`text-xs`
  uppercase-tracking style) for a compact, professional look; range errors show under their fields as
  today.
- **Rationale**: FR-014; reuses `TextField`/`SelectField` (labelled, error-associated) with a compact
  variant instead of new controls.

## R7. Column and card styling

- **Decision**: A `STATUS_STYLES` map next to `STATUS_TONES` in `badges.tsx` defines, per status, the
  column's top border and header dot (`slate-400`, `amber-500`, `emerald-500`) and a light header tint;
  the overdue marker keeps `OverdueBadge` (red). Cards are white with a subtle border and shadow; the
  description uses `line-clamp-2`; long titles use `break-words`. The column name is always shown, so
  color is never the only cue.
- **Rationale**: FR-002, FR-003; one source of truth for status colors, aligned with the existing badges.

## R8. Layout width and responsive behavior

- **Decision**: `AppLayout` widens its header and main container from `max-w-5xl` to `max-w-7xl`;
  other screens keep their own `max-w-*`. The board is `grid gap-4 lg:grid-cols-3`: one column per row
  below `lg` (stacked To do, In progress, Done), three side by side from `lg`.
- **Rationale**: Three columns need more room than the current 64 rem; stacking on narrow screens
  avoids horizontal page scrolling (FR-017, SC-005). Spec Assumptions.

## R9. Deleting from a card

- **Decision**: Extract the existing delete flow of `TaskDetailPage` into a `DeleteTaskButton`
  (confirmation, pending state, spec 003 outcomes) used by the card and by the detail panel. On the
  card it is a small icon-less text button labelled "Delete" with `aria-label="Delete <title>"`. After a
  confirmed delete, it invalidates `['tasks']` and navigates to `/tasks` + query string with the status
  "Task deleted." (from the panel this also closes it).
- **Rationale**: FR-004, FR-013; one implementation of the spec 003 behavior (rule of three not
  reached, but this is a move, not a copy).

## R10. Screens replaced

- **Decision**: `TaskListPage` → `TaskBoardPage` (+ `BoardColumn`, `TaskCard`); `TaskDetailPage`,
  `NewTaskPage`, `EditTaskPage` → `TaskDetailPanel`, `NewTaskPanel`, `EditTaskPanel` inside `TaskPanel`.
  `shared/ui/Pagination.tsx` is deleted (only the list used it). `TaskForm`, `TaskNotFound`,
  `StatusMessage`, `useTask`, `schemas`, `format`, `overdue`, `messages` are reused.
- **Rationale**: No dead code (constitution V); behavior already covered by tests moves with the code.

## R11. ADR

- **Decision**: ADR 0012 "Task board with a routed side panel" records R3 and R4 (routing and dialog
  approach for task details).
- **Rationale**: It changes how every task screen is reached, a significant UI architecture decision
  (constitution VII).
