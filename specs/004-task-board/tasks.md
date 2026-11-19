---

description: "Task list for feature 004 — Task Board"
---

# Tasks: Task Board

**Input**: Design documents from `specs/004-task-board/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/spa-routes.md](contracts/spa-routes.md),
[quickstart.md](quickstart.md)

**Tests**: REQUIRED. Constitution principle II (Test-First) applies:
- Each test task is written first and MUST fail (Red) before the implementation task that follows it
  (Green). An optional refactor closes the cycle.
- Commit each step separately (`test:`, `feat:`, `refactor:`), only after the repository owner
  confirms each change.
- Moving existing behavior to a new component keeps its tests: they move with it, adjusted to the new
  routes.
- Commits carry no co-author trailers.

**Organization**: Frontend only (`frontend/src`); the backend and the API do not change. The stories
follow the spec's priorities:
- **US1 (P1)** replaces the list with the board. The detail, create and edit screens stay as they are
  for now.
- **US2 (P1)** moves details and edit into the panel.
- **US3 (P2)** moves create into the panel.
- **US4 (P2)** adds delete on the card.
- **US5 (P3)** gives the filter bar its final layout.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: User story from spec.md (US1–US5)
- Paths are relative to `frontend/src/` unless they start with `docs/` or `specs/`.
- Test conventions:
  - `renderApp(path)` drives the real route table.
  - `stubFetch` answers each column by its `status=` query parameter.
  - `signedIn()` sets the session.
  - The jsdom `<dialog>` stand-in in `test/setup.ts` provides `showModal`/`close`/Escape.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Run `cd frontend; npm run check` and `dotnet test --solution TaskLane.slnx` on branch `004-task-board` and record that both pass (baseline; no file changes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Board address state and test helpers that every story uses.

- [X] T002 [P] Create `features/tasks/board-query.test.ts`, starting from the cases of `task-query.test.ts` (data-model.md "BoardQuery"):
  - Defaults are `sort=dueDate`, `direction=asc` and empty search and range, and they are left out of the address.
  - `status`, `page` and `pageSize` in the address are ignored.
  - Invalid values are dropped, with the same cases as today.
  - `columnParams(query, 'InProgress')` returns `{ status: 'InProgress', pageSize: '20', ...non-default values }`.
  - `withChanges` resets nothing page-related any more.
  - `hasFilters` is true only with a search or a range.
- [X] T003 Create `features/tasks/board-query.ts` from `task-query.ts`:
  - Fields: `search`, `dueFrom`, `dueTo`, `sort`, `direction`.
  - Functions: `parseBoardQuery`, `serializeBoardQuery`, `withChanges`, `hasFilters`, `columnParams(query, status)`, `BOARD_PAGE_SIZE = 20`, plus the existing `SORT_FIELDS`, `SORT_LABELS`, `isDate` and `daysBetween`.
  - Delete `task-query.ts` and `task-query.test.ts`, and update the imports in `TaskFilters.tsx` and `TaskListPage.tsx` so the build stays green.
  - T002 passes.
- [X] T004 [P] Add `boardSearch(query)` and `boardHref(path, search)` in `features/tasks/board-links.ts`: they return `''`/`'?…'` and `path + search`, so every board address keeps the query string (contracts/spa-routes.md). Also add `boardSearchFrom(locationSearch)`, which parses and reserializes it so that only board values are kept. It drops `status`, `page`, `pageSize` and any other parameter, and every link and redirect back to the board uses it. Cover all three with `features/tasks/board-links.test.ts`, written first; one case must check that `?status=InProgress&search=demo` becomes `?search=demo`
- [X] T005 [P] Add `STATUS_STYLES` to `features/tasks/badges.tsx` (research R7):
  - `ToDo`: `border-t-slate-400`, dot `bg-slate-400`, header `bg-slate-50`.
  - `InProgress`: `border-t-amber-500`, dot `bg-amber-500`, header `bg-amber-50`.
  - `Done`: `border-t-emerald-500`, dot `bg-emerald-500`, header `bg-emerald-50`.
  - It sits next to the unchanged `STATUS_TONES`.
  - Visual-only styling, so no class-name assertions: the column name stays the primary cue (FR-002),
    and the colors are checked in the manual walkthrough (T032).

**Checkpoint**: Address state ready; the current screens still work.

---

## Phase 3: User Story 1 - See my tasks as a board (Priority: P1) 🎯 MVP

**Goal**: `/tasks` shows three status columns with counts, colors, cards and "Load more".

**Independent Test**: With tasks in every status:
1. `/tasks` shows To do, In progress and Done with the right cards and counts.
2. A column with 25 tasks shows 20 plus "Load more", which adds the remaining 5.

### Tests for User Story 1 (write first, must fail)

- [X] T006 [US1] Create `features/tasks/TaskBoardPage.test.tsx` covering US1 scenarios 1–7 and FR-015:
  - The three lists named "To do", "In progress" and "Done", in order, each with only its tasks.
  - The headers show "2 tasks", "1 task" and so on.
  - A card shows the title link (`/tasks/<id>` + query string), at most the description excerpt, "Due <date>" or "No due date", and "Overdue".
  - Requests are `/api/tasks?status=ToDo&pageSize=20` and the same for each status.
  - "Load more" requests `page=2` for that column only, appends the items and disappears at the end; it is disabled with `aria-busy` while loading.
  - Empty states:
    - A column with no tasks shows "No tasks".
    - No tasks at all shows "You have no tasks yet." with a "Create your first task" link.
    - A search with no matches shows "No tasks match your search." with a clear button.
  - While a column is loading it shows "Loading tasks…" (FR-018).
  - A failing column shows the service message while the other columns still render.
  - The status message passed in the navigation state is shown.
  - A "New task" link points to `/tasks/new`.
  - Port the remaining applicable list tests from `TaskListPage.test.tsx`, such as the Tasks navigation link and the API refusal message.

### Implementation for User Story 1

- [X] T007 [P] [US1] Create `features/tasks/TaskCard.tsx`:
  - An `<li>` with a white card (border, subtle shadow) holding the title `<Link>` to `boardHref('/tasks/<id>', search)` with `break-words`.
  - The description in `line-clamp-2` when present.
  - The due date via `formatDate`, or "No due date", and `OverdueBadge` when `isOverdue`.
- [X] T008 [P] [US1] Create the `features/tasks/useBoardColumn.ts` hook `useBoardColumn(status, query)`. It wraps `useInfiniteQuery` (key `['tasks', 'board', status, params, accessToken]`, `listTasks(columnParams(...) + page)`, `getNextPageParam` from `page * pageSize < totalCount`) and returns `{ tasks, totalCount, hasMore, loadMore, isLoading, isLoadingMore, error }`.

  Then create the presentational `features/tasks/BoardColumn.tsx`, which receives `status`, `query` and that result as props:
  - Layout: a `<section>` with a top border and header tint from `STATUS_STYLES`. The header has the dot, an `<h2>` with the name, and the count ("N task(s)").
  - Contents: `<ul aria-label={label}>` of `TaskCard`, then "Load more" / "Loading more…".
  - States: "Loading tasks…", "No tasks", and errors (a `400` field message or the service message).
- [X] T009 [US1] Create `features/tasks/TaskBoardPage.tsx`:
  - The header ("Tasks" `<h1>` and the "New task" link with the query string), `StatusMessage` and `TaskFilters`.
  - The board: `grid gap-4 lg:grid-cols-3` with the three `BoardColumn`, then `<Outlet />` for the panel.
  - It calls `useBoardColumn` once per status and passes each result to its `BoardColumn`. The board-level empty states come from the three `totalCount` values once all three first pages have loaded: "You have no tasks yet." with no filters, "No tasks match your search." with filters (research R2).
  - `TaskFilters`: remove the Status select and the `mode` remnants so it uses `BoardQuery`.
  - T006 passes.
- [X] T010 [US1] Route `/tasks` to `TaskBoardPage` in `app/routes.tsx`, keeping `tasks/new`, `tasks/:id` and `tasks/:id/edit` as sibling routes for now. Delete `TaskListPage.tsx`, `TaskListPage.test.tsx` and `shared/ui/Pagination.tsx`
- [X] T011 [US1] Widen `shared/layout/AppLayout.tsx` header and main containers from `max-w-5xl` to `max-w-7xl` (research R8); run `npm run check`

**Checkpoint**: The board works end to end; opening a card still goes to the existing detail page.

---

## Phase 4: User Story 2 - Open a task in a side panel (Priority: P1)

**Goal**: Details and edit open in a routed side panel over the board.

**Independent Test**:
1. Open a card: a dialog named after the task shows its details over the board.
2. Escape, Close and Back return to the board with the same query string, and focus returns to the card.
3. Reloading `/tasks/<id>` opens the panel.
4. Editing in the panel updates the card.

### Tests for User Story 2 (write first, must fail)

- [X] T012 [P] [US2] Create `features/tasks/TaskPanel.test.tsx`:
  - `/tasks/abc?search=demo` renders the board (the column lists are present) and a dialog named "Prepare demo".
  - "Close" and Escape replace the address with `/tasks?search=demo` (history action `REPLACE`), so a following Back does not reopen the panel (U1). This holds whether the panel was opened from a card or directly.
  - `router.navigate(-1)` after opening from the board closes the panel.
  - A task hidden by the current search still opens in the panel when its address is opened (G1).
  - Focus returns to the card link after closing.
  - `/tasks/missing` shows "Task not found" in the panel with a link back to the board.
- [X] T013 [P] [US2] Create `features/tasks/TaskDetailPanel.test.tsx` by moving the cases of `TaskDetailPage.test.tsx` onto `/tasks/abc`:
  - Every detail, the placeholders and "Edit" pointing to `/tasks/abc/edit` with the query string.
  - The status message, the 404 and the load failure.
  - The delete flow from the panel: confirmation, the panel closes, the board shows "Task deleted.", plus the 404 alert, the pending state and the failure message.
- [X] T014 [P] [US2] Create `features/tasks/EditTaskPanel.test.tsx` by moving the cases of `EditTaskPage.test.tsx` onto `/tasks/abc/edit`:
  - The form opens inside the dialog named "Edit task".
  - Saving shows the details with "Task updated.", and the card moves to the column of its new status.
  - Validation and server errors, and the 404 alert, which goes to `/tasks` with "This task no longer exists.".
  - New: "Cancel" returns to `/tasks/abc` unchanged.

### Implementation for User Story 2

- [X] T015 [US2] Create `features/tasks/TaskPanel.tsx`, a layout route (research R4):
  - A native `<dialog>` opened with `showModal()` on mount, with `aria-labelledby` set to a `useId()` passed to the content via `<Outlet context={{ titleId }} />`.
  - Classes: right-aligned full height `sm:w-[32rem]`, full screen below `sm`, `backdrop:bg-slate-900/20`.
  - A "Close" button. Close and the `cancel` event (Escape, with `preventDefault`) call `navigate(boardHref('/tasks', boardSearchFrom(location.search)), { replace: true })`.
  - Saving, cancelling, and deleting from the panel also navigate with `replace`, so Back never returns to a closed panel or a submitted form. This is simpler than the `fromBoard` flag first proposed for U1, which would have sent Back from the details to the edit form after a save.
  - On unmount, focus returns to the element that was active when it opened if it is still connected, otherwise to the board `<h1>`.
- [X] T016 [US2] Create `features/tasks/DeleteTaskButton.tsx` by extracting `DeleteAction` from `TaskDetailPage.tsx` (research R9):
  - Props: the task, `accessToken`, `onFailed` and an optional `label` for the accessible name.
  - On success or `404`, it invalidates `['tasks']` and navigates to `boardHref('/tasks', boardSearchFrom(location.search))` with `{ status: taskMessages.deleted }` or `{ alert: taskMessages.noLongerExists }`.
- [X] T017 [US2] Create `features/tasks/TaskDetailPanel.tsx` from `TaskDetailPage.tsx`:
  - It renders the `<h2 id={titleId}>` title, `StatusMessage`, the detail list, "Edit" (`boardHref('/tasks/<id>/edit', search)`) and `DeleteTaskButton`.
  - It drops the "Back to tasks" link, because the panel has Close.
  - Delete `TaskDetailPage.tsx` and `TaskDetailPage.test.tsx`. T013 passes.
- [X] T018 [US2] Create `features/tasks/EditTaskPanel.tsx` from `EditTaskPage.tsx`:
  - The `<h2 id={titleId}>Edit task</h2>`, then `TaskForm` plus a "Cancel" link back to the details.
  - Navigation keeps the query string.
  - Delete `EditTaskPage.tsx` and `EditTaskPage.test.tsx`. T014 passes.
- [X] T019 [US2] In `app/routes.tsx`, nest the panel under `/tasks`: `{ path: 'tasks', element: <TaskBoardPage />, children: [{ element: <TaskPanel />, children: [{ path: ':id', … }, { path: ':id/edit', … }] }] }`. Keep `tasks/new` as a sibling until US3. T012 passes. Update `app/routes.test.tsx` (`/tasks/deleted` shows "Task not found" inside the panel)

**Checkpoint**: Details and edit live in the panel; create still uses its page.

---

## Phase 5: User Story 3 - Create a task from the board (Priority: P2)

**Goal**: Create in the panel from "New task" or a column's "+", with the status preset.

**Independent Test**:
1. "+" on In progress opens the create form with "In progress" selected.
2. Creating adds the card there, and the panel shows the new task.
3. Reloading `/tasks/new?status=Done` keeps the preset.

### Tests for User Story 3 (write first, must fail)

- [ ] T020 [US3] Create `features/tasks/NewTaskPanel.test.tsx` by moving the cases of `NewTaskPage.test.tsx` onto `/tasks/new`:
  - The dialog is named "New task" and its status defaults to "To do".
  - `/tasks/new?status=InProgress` presets "In progress", and an invalid `status` falls back to "To do".
  - Creating posts the values, then shows `/tasks/<id>` with "Task created.", and the card appears in its column.
  - Validation, the service error and keeping the values all behave as before.
  - Closing the panel creates nothing.
  - Created with the board filtered by `search=zzz` (the new task does not match): the panel shows the new task, the column counts stay the same, and no card appears (G1).
  - Extend `TaskBoardPage.test.tsx`: each column header has a "New task in <name>" link to `/tasks/new?status=<status>` that keeps the query string.

### Implementation for User Story 3

- [ ] T021 [US3] Create `features/tasks/NewTaskPanel.tsx` from `NewTaskPage.tsx`:
  - An `<h2 id={titleId}>New task</h2>`.
  - `TaskForm` with `defaultValues = { ...EMPTY_TASK_FORM, status: preset }`, where `preset` is read from `?status`, validated against `TASK_STATUSES`, and defaults to `ToDo`.
  - On success it navigates to `boardHref('/tasks/<id>', boardSearchFrom(location.search))` with "Task created.", so the `status` preset parameter is dropped.
  - Delete `NewTaskPage.tsx` and `NewTaskPage.test.tsx`.
- [ ] T022 [US3] Add the "+" link (`aria-label="New task in <name>"`) to the column header in `features/tasks/BoardColumn.tsx`. Move `new` under the `TaskPanel` children in `app/routes.tsx`, before `:id`. T020 passes

**Checkpoint**: All task screens live in the board and panel.

---

## Phase 6: User Story 4 - Delete a task from the board (Priority: P2)

**Goal**: Each card offers "Delete" with the permanent-deletion confirmation, without opening the panel.

**Independent Test**: Delete from a card: the confirmation appears, the card disappears, the count goes down by one, and "Task deleted." shows. Cancel changes nothing.

### Tests for User Story 4 (write first, must fail)

- [ ] T023 [US4] Extend `features/tasks/TaskBoardPage.test.tsx`:
  - Each card has a "Delete <title>" button.
  - The confirmation is "Delete this task permanently? This can't be undone." and no panel opens.
  - Confirm sends `DELETE /api/tasks/<id>`, refetches the columns, shows "Task deleted." and stays on `/tasks` with the query string.
  - Cancel sends nothing.
  - A `404` shows "This task no longer exists.", and a failure shows the service message.

### Implementation for User Story 4

- [ ] T024 [US4] Add `DeleteTaskButton` to `features/tasks/TaskCard.tsx` (small secondary text button, accessible name "Delete <title>"); on failure show the service message in the card (T023 passes)

**Checkpoint**: Deletion is available from the card and the panel.

---

## Phase 7: User Story 5 - Find tasks with the filter bar (Priority: P3)

**Goal**: One professional horizontal filter bar that applies to every column.

**Independent Test**:
1. Search, range and sort change every column's request and count.
2. The address keeps them across a reload.
3. "Clear search and filters" resets them.
4. There is no Status control.

### Tests for User Story 5 (write first, must fail)

- [ ] T025 [P] [US5] Update `features/tasks/TaskFilters.test.tsx`:
  - The controls appear in the order Search, Due from, Due to, Apply, Sort by, Order, and "Clear search and filters" when a filter is active.
  - There is no "Status" control.
  - The sort options are exactly Due date, Created and Title.
  - The debounce, range validation and messages behave as before.
  - The bar is a single `role="search"` region.
- [ ] T026 [P] [US5] Extend `features/tasks/TaskBoardPage.test.tsx` with US5 scenarios 2–6:
  - `/tasks?search=demo&sort=title&direction=desc` sends those values in all three column requests.
  - A range is applied to all columns, and a `400` range message is shown.
  - Typing a search updates the address after the debounce and resets loaded batches.
  - "Clear search and filters" keeps only the non-filter values.
  - Old `status=Done&page=3` values are ignored.

### Implementation for User Story 5

- [ ] T027 [US5] Restyle `features/tasks/TaskFilters.tsx` as one bar (research R6):
  - A `role="search"` container with a white background, border and rounded corners.
  - Inside, a `flex flex-wrap items-end gap-3` row: Search grows (`min-w-48 flex-1`), the date fields are fixed width, then Apply, Sort by, Order and "Clear search and filters".
  - Compact labels and inputs. If needed, add a `size="sm"` option to `shared/ui/TextField.tsx` and `SelectField.tsx` without changing their defaults.
  - T025 and T026 pass.

**Checkpoint**: All five stories complete.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T028 [P] Write `docs/adr/0012-task-board-routed-side-panel.md`:
  - Context: the list and separate pages.
  - Decision: nested routes and the native `<dialog>` panel, `useInfiniteQuery` per column, no API change.
  - Alternatives: a query-parameter panel, a modal without an address, a drawer library, a board endpoint.
  - Consequences: three requests per filter change; the panel is modal, so the board is inert while it is open.
  - Add the row to `docs/adr/README.md`.
- [ ] T029 [P] Add a "Replaced by [spec 004](../../004-task-board/contracts/spa-routes.md) for the task screens" banner under the title of `specs/002-task-management/contracts/spa-routes.md`; update the Tasks bullets in `README.md` (board, side panel, Load more) and the SPA notes in `docs/architecture.md` if they describe the list
- [ ] T030 [P] Search `frontend/src` for leftovers: `TaskListPage`, `NewTaskPage`, `EditTaskPage`, `TaskDetailPage`, `task-query`, `Pagination`, `page=` in task code, and "Back to tasks" links inside the panel. Remove dead code and unused exports, and check that `npm run lint` has zero warnings
- [ ] T031 Run the full gate:
  - `cd frontend; npm run check`.
  - `dotnet build TaskLane.slnx`.
  - `dotnet format TaskLane.slnx --verify-no-changes`.
  - `./scripts/test-coverage.ps1`; the backend is unchanged and its coverage must stay above the gates.
- [ ] T032 Run the manual walkthrough in `specs/004-task-board/quickstart.md`, on desktop and at 360 px, with the browser console open. Report the results to the repository owner

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)** → **Foundational (T002–T005)** → **US1** → **US2** → **US3**, then **US4** and **US5**, then **Polish**.
- US2 needs the board from US1, where the panel sits.
- US3 reuses the `TaskPanel` from US2.
- US4 reuses the `DeleteTaskButton` from US2.
- US5 only needs US1, so it can run in parallel with US2–US4 if staffed. In practice it goes last
  because it shares `TaskBoardPage.test.tsx`.

### Within Each Story

- Tests first (Red), then components, then routes.
- Removing an old page happens in the same task that moves its tests, so the suite never loses
  coverage.

### Parallel Opportunities

- Foundational: T002, T004 and T005 can run in parallel.
- US1: T007 and T008 can run in parallel; both are needed by T009.
- US2: the tests T012, T013 and T014 can run in parallel.
- US5: T025 and T026 can run in parallel.
- Polish: T028, T029 and T030 can run in parallel.

## Parallel Example: User Story 2

```text
Together (tests, Red):
  T012 TaskPanel.test.tsx    T013 TaskDetailPanel.test.tsx    T014 EditTaskPanel.test.tsx
Then (Green, sequential):
  T015 TaskPanel → T016 DeleteTaskButton → T017 TaskDetailPanel → T018 EditTaskPanel → T019 routes
```

## Implementation Strategy

### MVP First (User Story 1)

Phases 1–3 alone give the user the board: columns, colors, counts, cards and "Load more". Details,
create and edit still work on their existing pages. Stop and validate with the US1 independent test.

### Incremental Delivery

1. US1: the board (MVP).
2. US2: the side panel for details and edit, which is the core of the redesign.
3. US3: create in the panel with the preset status.
4. US4: delete from the card.
5. US5: the final filter bar.
6. Polish: ADR 0012, docs, the full gate and the manual walkthrough.

Each phase ends with `npm run check` green before the next starts. Each Red/Green step is presented to
the repository owner and committed only after confirmation.

## Notes

- No backend files change. If implementation reveals a needed API change, stop: it is out of scope for
  spec 004.
- Keep `TaskForm`, `TaskNotFound`, `StatusMessage`, `useTask`, `messages`, `format`, `overdue` and
  `schemas` unchanged unless a task names them.
