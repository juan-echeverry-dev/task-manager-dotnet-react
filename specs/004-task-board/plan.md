# Implementation Plan: Task Board

**Branch**: `004-task-board` | **Date**: 2026-11-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/004-task-board/spec.md`

## Summary

Replace the paginated task list with a kanban board of three status columns, and move task details,
create and edit into a side panel that has its own address. Frontend only: the API of specs 002/003 is
reused as is.

Technical approach:
- **Columns:** each column is the existing list endpoint filtered by its status, loaded with TanStack
  `useInfiniteQuery` and a "Load more" button.
- **Panel:** nested routes under `/tasks` render a native `<dialog>` panel over the mounted board. The
  board's search, range and sort travel in the query string of every board address.
- **Filter bar:** becomes one wrapping row, without the status filter.
- **Delete:** the delete flow is extracted so both the card and the panel use it.
- **Styling:** the status colors come from the existing badge palette.

## Technical Context

**Language/Version**: TypeScript 7 (strict) on Node ≥ 24; backend unchanged (C# 14 / .NET 10)

**Primary Dependencies**: No new packages. Reuses React 19, React Router 8 (nested routes, outlet
context), TanStack Query 5 (`useInfiniteQuery`), React Hook Form + Zod, Tailwind 4 (`line-clamp`).

**Storage**: N/A (no persisted changes).

**Testing**: Vitest + Testing Library with the existing helpers:
- `renderApp`, which uses the real route table.
- `stubFetch`.
- The jsdom `<dialog>` stand-in.

There are no backend changes, so the backend suites just have to stay green.

**Target Platform**: Evergreen desktop and mobile browsers, 360 px and up.

**Project Type**: Web application (React SPA over the existing ASP.NET Core API).

**Performance Goals**: SC-001 (counts visible < 2 s locally) and SC-004 (500 tasks, filter change
< 1 s). These are three parallel requests of 20 items.

**Constraints**:
- No API change.
- No horizontal page scrolling at 360 px.
- Keyboard operable, with no console warnings.
- Lint has zero warnings.

**Scale/Scope**:
- 1 screen replaced by 1 board and 3 panel contents.
- About 10 frontend files added or changed, and 5 removed.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| I. Spec-driven | Spec 004 amends spec 002's presentation explicitly (table "Relationship to Specs 002 and 003") | ✅ |
| II. Test-first | Every behavior starts with a failing Vitest spec through `renderApp` (real routes) | ✅ |
| III. Dependency rule | Backend untouched; architecture tests stay green | ✅ |
| IV. Vertical slices | All changes inside `frontend/src/features/tasks` plus the route table and layout width; shared UI only for primitives | ✅ |
| V. Simplicity | No new dependency. Native `<dialog>` instead of a drawer library, built-in `useInfiniteQuery`, no new API (research R1–R4). `Pagination` removed, since nothing uses it any more | ✅ |
| VI. Security | No change to auth; all routes stay under `RequireAuth`; ownership enforced by the API as before | ✅ |
| VII. Quality | Strict TS, zero lint warnings, no console warnings; ADR 0012 for the routed panel | ✅ |
| Workflow | Branch `004-task-board`; commits only after owner confirmation | ✅ |

**Post-design re-check (after Phase 1)**: unchanged. All gates pass; Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/004-task-board/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1 (client view state)
├── quickstart.md        # Phase 1
├── contracts/
│   └── spa-routes.md    # Board, panel, filter bar, states
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (frontend)

```text
frontend/src/
├── app/routes.tsx                         # /tasks → TaskBoardPage with TaskPanel children (new, :id, :id/edit)
├── shared/
│   ├── layout/AppLayout.tsx               # max-w-5xl → max-w-7xl
│   └── ui/Pagination.tsx                  # removed (no users left)
└── features/tasks/
    ├── board-query.ts (+ test)            # replaces task-query.ts: board address state, column params
    ├── TaskBoardPage.tsx (+ test)         # replaces TaskListPage: header, filter bar, columns, <Outlet/>
    ├── useBoardColumn.ts                  # useInfiniteQuery per status: tasks, count, hasMore, loadMore
    ├── BoardColumn.tsx                    # presentational: header, cards, Load more, column states
    ├── TaskCard.tsx                       # title link, excerpt, due date, overdue, DeleteTaskButton
    ├── TaskFilters.tsx (+ test)           # single wrapping row, no status filter
    ├── badges.tsx                         # + STATUS_STYLES (column accents)
    ├── DeleteTaskButton.tsx               # extracted delete flow (spec 003 outcomes)
    ├── TaskPanel.tsx (+ test)             # <dialog> shell: open/close, Escape, Back, focus return
    ├── TaskDetailPanel.tsx (+ test)       # replaces TaskDetailPage
    ├── NewTaskPanel.tsx (+ test)          # replaces NewTaskPage (status preset)
    ├── EditTaskPanel.tsx (+ test)         # replaces EditTaskPage (+ Cancel)
    ├── board-links.ts                     # helpers keeping the query string on board addresses
    └── TaskForm.tsx, TaskNotFound.tsx, StatusMessage.tsx, useTask.ts, messages.ts, format.ts,
        overdue.ts, schemas.ts, tasks-api.ts   # reused (small text additions only)

docs/adr/0012-task-board-routed-side-panel.md          # new; README table updated
specs/002-task-management/contracts/spa-routes.md      # "Replaced by spec 004" banner
```

Removed: `TaskListPage.tsx`, `TaskDetailPage.tsx`, `NewTaskPage.tsx` and `EditTaskPage.tsx`, with their
tests. Their behavior moves to the new tests. Also removed: `task-query.ts` (+ test) and
`shared/ui/Pagination.tsx`.

**Structure Decision**: The existing `frontend/` SPA, inside the `tasks` feature folder. The backend
is unchanged.

## Implementation Notes

- **Order (TDD):**
  1. `board-query` and the board with its columns (US1).
  2. The panel shell and details, then edit (US2).
  3. Create (US3).
  4. Card delete (US4).
  5. The filter bar (US5).
  6. Polish: ADR, banner, docs, and the full gate.
- **Route table sketch:**
  `{ path: 'tasks', element: <TaskBoardPage/>, children: [{ element: <TaskPanel/>, children: [{ path: 'new' }, { path: ':id' }, { path: ':id/edit' }] }] }`.
- **Board header width:** the AppLayout change also widens the header, so it stays aligned with
  the board.
- **Status messages:** the board's `StatusMessage` reads the state on `/tasks`, and the panel
  content shows its own messages (created and updated).
- **Tests:** `renderApp('/tasks/abc?search=demo')` drives real routing. Assertions:
  - The dialog is found by role `dialog` and its name.
  - Columns are found by the list name ("To do", …).
  - `stubFetch` answers by `status=` in the URL.

## Complexity Tracking

No violations.
