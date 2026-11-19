# Quickstart: validate feature 002

This guide proves the feature works end to end. The contracts are in
[contracts/tasks-api.md](contracts/tasks-api.md) and [contracts/spa-routes.md](contracts/spa-routes.md);
the entity and validation rules are in [data-model.md](data-model.md).

## Prerequisites

Feature 001 merged, a fresh clone on branch `002-task-management`, `dotnet tool restore`, and
`cd frontend; npm ci`.

## 1. Automated gates

```powershell
dotnet build TaskLane.slnx                      # 0 warnings
dotnet format TaskLane.slnx --verify-no-changes
./scripts/test-coverage.ps1                     # ≥ 90 % Domain+Application, ≥ 80 % overall
cd frontend; npm run check
```

| Layer | Must prove |
|---|---|
| Unit | `TaskItem` (create defaults, trimming, limits, due-date range, update, delete/restore idempotency, update on a deleted task throws); every validator rule in data-model; handlers: success, 400, 404 (missing or foreign), 409 (deleted) |
| Infrastructure | Migration creates `Tasks` with FK and indexes; repository: owner scoping, active vs deleted lists, search (title, description, case, `%`/`_` escaping), status filter, inclusive range filter excluding tasks without a due date, every sort field and direction with nulls last and tie-breakers, paging and totals, a page past the end; 500 tasks list in under 1 s; demo seeder (once, not after all tasks are deleted, never modifies) |
| Api | Every status code in the contract; ownership matrix (another user's task → 404 identical to a missing one on GET, PUT, DELETE and restore); query validation errors keyed by parameter; settings returns the configured maximum (overridden in a test to prove it is configurable) |
| Architecture | Tasks (Domain, Application, Api) does not depend on Identity; existing rules stay green |
| Frontend | `task-query` parse and serialize (defaults, invalid values dropped, page reset); `isOverdue`; forms (validation, pending, server errors); detail (active vs deleted read-only); delete confirmation; restore; lists (rows, badges, empty states, pagination, range filter with the configured maximum) |

## 2. Manual API check (Development)

```powershell
dotnet run --project src/TaskLane.Api --launch-profile http
```

1. Sign in as `demo@tasklane.dev` / `TaskLane-Demo-2026` and use the token as the bearer value.
2. `GET /api/tasks` → six demo tasks; tasks without a due date come last.
3. `POST /api/tasks` with only a title → `201`, status `ToDo`.
4. `PUT /api/tasks/{id}` with an empty title → `400 errors.title`. With valid values → `200`.
5. `DELETE /api/tasks/{id}` → `204`. The task is no longer in `GET /api/tasks` and appears in
   `GET /api/tasks/deleted`. `PUT` on it → `409`. `POST /api/tasks/{id}/restore` → `200`.
6. `GET /api/tasks?dueFrom=2026-01-01&dueTo=2026-04-02` → `400` ("at most 90 days").
   `…&dueTo=2026-04-01` → `200`.
7. Register a second account; its `GET /api/tasks/{demo task id}` → `404`.
8. Restart with `Tasks__DueDateRangeMaxDays=30` → `GET /api/tasks/settings` returns 30.
9. Restart normally → the demo tasks are not duplicated.

## 3. Manual SPA check

```powershell
cd frontend; npm run dev                        # http://localhost:5173
```

1. Sign in as the demo account and open "Tasks": the overdue task shows the "Overdue" badge, the
   Done task does not.
2. Create a task, edit it, change its status, then remove its due date.
3. Search, filter by status, apply a due-date range, change the sort, and move pages. Reload and
   check that the same state is restored from the URL.
4. Try a 91-day range: the message appears and the list does not change.
5. Delete a task: confirm the dialog, find it in "Deleted tasks", open it read-only, and restore it.
6. At 360 px width there is no horizontal scroll, everything works with the keyboard only, and the
   console stays clean.
