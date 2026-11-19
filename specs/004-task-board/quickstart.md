# Quickstart: Validate the Task Board

## Prerequisites

As in the README: .NET 10 SDK, Node ≥ 24, `npm ci` in `frontend/`. Start the API and the SPA
(`dotnet run --project src/TaskLane.Api --launch-profile http`, then `cd frontend; npm run dev`) and
sign in with the demo account.

## Automated checks

```powershell
dotnet build TaskLane.slnx                 # unchanged backend, must stay green
dotnet test --solution TaskLane.slnx
cd frontend; npm run check                 # typecheck, lint, tests, build
```

| Spec item | Covered by |
|---|---|
| US1, FR-001 to FR-006, FR-015, SC-006 | `TaskBoardPage.test.tsx`, `board-query.test.ts` |
| US2, FR-007, FR-009 to FR-012, FR-017 | `TaskPanel.test.tsx`, `TaskDetailPanel.test.tsx`, `EditTaskPanel.test.tsx` |
| US3, FR-008 | `NewTaskPanel.test.tsx` |
| US4, FR-013 | `TaskBoardPage.test.tsx` (card delete), `TaskDetailPanel.test.tsx` |
| US5, FR-014, FR-016 | `TaskFilters.test.tsx`, `TaskBoardPage.test.tsx` |

## Manual walkthrough (desktop, then 360 px)

1. Open `/tasks`: three columns To do, In progress, Done with slate, amber and emerald accents and
   counts; cards show title, two description lines, due date, red "Overdue" where it applies.
2. Create 21+ tasks in one status (or lower the data): the column shows 20 and "Load more"; it adds the
   rest without touching the other columns.
3. Open a card: the panel slides in at the right, the board stays visible. Press Escape: it closes and
   focus is back on the card. Open it again and use Back: it closes too.
4. Reload `/tasks/<id>`: the board opens with the panel.
5. Edit the task, change its status, save: the panel shows "Task updated." and the card moved columns;
   counts changed.
6. Use "+" on "In progress": the create form has "In progress" selected; create: the card appears there.
7. Delete from a card and from the panel: confirmation, card gone, count down, "Task deleted.".
8. Type a search, set a range, change sort: all columns follow; reload keeps them; "Clear search and filters" resets.
9. At 360 px: the filter bar wraps, columns stack, the panel is full screen, no horizontal scrolling;
   the browser console has no errors or warnings.
