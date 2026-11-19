# Quickstart: Validate Permanent Task Deletion

## Prerequisites

As in the README: .NET 10 SDK, Node ≥ 24, `npm ci` in `frontend/`. Local development seeds the demo
account (feature 001) and its sample tasks.

## Automated checks

```powershell
dotnet build TaskLane.slnx
dotnet format TaskLane.slnx --verify-no-changes
./scripts/test-coverage.ps1          # all backend tests + coverage gate
cd frontend; npm run check           # typecheck, lint, tests, build
```

Expected: everything green, zero warnings. The suites cover:

| Spec item | Where |
|---|---|
| US1, FR-001 to FR-004, SC-001, SC-002 | API tests for `DELETE`, `GET`, `PUT` after deletion and on foreign tasks; repository tests for `DeleteOwnedAsync` and `UpdateAsync` on a removed row; handler unit tests |
| US2, FR-005, SC-003 | API tests: `GET /api/tasks/deleted` and `POST /api/tasks/{id}/restore` answer `404`; SPA tests: no "Deleted tasks" link, no "Restore", `/tasks/deleted` shows "Task not found" |
| US3, FR-006, SC-004 | Infrastructure test applying the migration to a database with active and soft-deleted rows |
| FR-007 | SPA test: delete button disabled with `aria-busy` while pending |
| FR-008 | Seeder tests (unchanged rule) |

## Manual walkthrough

1. Start the API and the SPA (README). Sign in with the demo account.
2. Open a task → **Delete**. The dialog says the deletion is permanent. Press **Cancel**: nothing changes.
3. **Delete** again and confirm: you land on `/tasks` with "Task deleted."; the task and its count are gone.
4. Use the browser **Back** button to the task's address: "Task not found".
5. Open a task in two tabs. Delete it in the first; in the second press **Edit → Save**: you land on
   `/tasks` with "This task no longer exists."
6. Visit `/tasks/deleted`: "Task not found" with "Back to tasks". No screen shows "Deleted tasks" or
   "Restore".

## Migration on an existing database

1. On `main` (before this feature), delete one task in the app so a soft-deleted row exists.
2. Switch to this branch and start the API (migrations apply on startup in Development).
3. Expected: active tasks are unchanged; the soft-deleted task no longer exists, and the `Tasks`
   table no longer has a `DeletedAt` column.
