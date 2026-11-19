# Implementation Plan: Permanent Task Deletion

**Branch**: `003-permanent-task-deletion` | **Date**: 2026-11-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-permanent-task-deletion/spec.md`

## Summary

Replace the recoverable (soft) deletion of spec 002 with permanent deletion and remove everything
built for recovery: the deleted-tasks list, restore, and the read-only view of deleted tasks.

Technical approach:
- **Domain:** `TaskItem` loses `DeletedAt`, `IsDeleted`, `Delete()` and `Restore()`.
- **Persistence:** `DeleteTaskHandler` removes the row with one owner-scoped statement through a new
  repository method. `UpdateAsync` reports a row that disappeared mid-request, so the edit answers
  `404` instead of `500`.
- **Migration:** one migration deletes the rows already soft-deleted, then drops the column and its
  indexes.
- **API:** the deleted-list and restore endpoints, `409` and `deletedAt` are removed.
- **SPA:** the `deleted` list mode, the `/tasks/deleted` route, the restore action and the related
  messages are removed. The confirmation now says the deletion is permanent.
- **Docs:** ADR 0011 records the decision, and the spec 002 documents carry an "amended by 003" banner.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS; TypeScript 7 (strict) on Node ≥ 24

**Primary Dependencies**: No new packages. The plan reuses ASP.NET Core 10, EF Core 10 + SQLite,
FluentValidation, React 19, React Router 8 and TanStack Query 5.

**Storage**: SQLite. The `Tasks` table loses `DeletedAt` and two indexes are replaced (see
[data-model.md](data-model.md)).

**Testing**: The existing pyramid:
- Unit tests for the domain and handlers.
- Infrastructure tests on real SQLite: the repository and the migration data cleanup.
- API tests with `WebApplicationFactory`.
- Architecture tests.
- Vitest for the SPA.

**Target Platform**: Local developer machine and GitHub Actions; evergreen browsers.

**Project Type**: Web application (ASP.NET Core Web API + React SPA).

**Performance Goals**: Unchanged from spec 002 (SC-004). The new `(OwnerId, DueDate)` and
`(OwnerId, CreatedAt)` indexes cover the list orders.

**Constraints**:
- Ownership is checked in every statement, including the delete.
- Zero warnings.
- Coverage ≥ 90 % for Domain + Application and ≥ 80 % overall.

**Scale/Scope**:
- **Removed:** 2 endpoints, 1 use case, 1 column and 1 SPA route.
- **Changed:** 2 use cases (Delete, Update) and 3 SPA screens (list, detail, edit).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| I. Spec-driven | Spec 003 amends spec 002 explicitly (table "Relationship to Spec 002"); no behavior without spec | ✅ |
| II. Test-first | Each behavior change starts with a failing test: `DELETE` removes and second call is `404`; `PUT` after delete is `404`; removed endpoints are `404`; migration cleanup; SPA confirmation text and removed screens. The migration itself is exempt from test-first, but its data cleanup is still covered | ✅ |
| III. Dependency rule | Application uses only the `ITaskRepository` port; `DbUpdateConcurrencyException` is caught in Infrastructure, never seen by Application | ✅ |
| IV. Vertical slices | Delete and Update slices change in place; the `RestoreTask` slice is removed whole | ✅ |
| V. Simplicity | No new abstraction: `Result<Guid>` instead of a new non-generic `Result` (research R2); port methods changed only where behavior needs it | ✅ |
| VI. Security | Delete statement is owner-scoped; foreign and missing tasks remain indistinguishable (`404`) | ✅ |
| VII. Quality | Removing dead code (deleted mode, `deletedAt` sort, messages); ADR 0011 recorded | ✅ |
| Workflow | Branch `003-permanent-task-deletion`; commits only after owner confirmation | ✅ |

**Post-design re-check (after Phase 1)**: unchanged. All gates pass; Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/003-permanent-task-deletion/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── tasks-api.md     # API delta vs spec 002
│   └── spa-routes.md    # SPA delta vs spec 002
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── TaskLane.Domain/Tasks/TaskItem.cs                         # remove DeletedAt, IsDeleted, Delete, Restore
├── TaskLane.Application/
│   ├── DependencyInjection.cs                                # drop RestoreTaskHandler
│   └── Tasks/
│       ├── Ports/ITaskRepository.cs                          # + DeleteOwnedAsync; UpdateAsync → bool
│       ├── Ports/TaskListQuery.cs                            # remove Deleted, TaskSortField.DeletedAt
│       ├── DeleteTask/DeleteTaskHandler.cs                   # Result<Guid>, permanent
│       ├── UpdateTask/UpdateTaskHandler.cs                   # no Deleted check; false → NotFound
│       ├── GetTask/GetTaskHandler.cs                         # doc comment only
│       ├── ListTasks/{ListTasksQuery,ListTasksHandler,ListTasksValidator,ListSorting}.cs
│       ├── RestoreTask/                                      # removed
│       ├── TaskErrors.cs                                     # remove Deleted
│       └── TaskResponse.cs                                   # remove DeletedAt
├── TaskLane.Infrastructure/
│   ├── Tasks/TaskRepository.cs                               # DeleteOwnedAsync, UpdateAsync, no deleted filter/sort
│   ├── Tasks/DemoTasksSeeder.cs                              # comment + sample text
│   └── Persistence/
│       ├── Configurations/TaskItemConfiguration.cs           # drop DeletedAt, new indexes
│       └── Migrations/<timestamp>_RemoveTaskSoftDelete.cs    # cleanup + schema
└── TaskLane.Api/Tasks/TasksController.cs                     # remove ListDeleted, Restore, 409; deleted flag

tests/
├── TaskLane.UnitTests/Tasks/                                 # TaskItemTests, Delete/Update/List handler tests; RestoreTask tests removed
├── TaskLane.Infrastructure.Tests/Tasks/                      # repository, persistence, query, seeder, migration cleanup
└── TaskLane.Api.Tests/Tasks/                                 # DeleteAndRestoreTaskTests → DeleteTaskTests; Find/Create tests

frontend/src/
├── app/routes.tsx                                            # remove tasks/deleted
└── features/tasks/
    ├── tasks-api.ts                                          # remove deletedAt, listDeletedTasks, restoreTask
    ├── task-query.ts                                         # remove TaskListMode and deletedAt sort
    ├── TaskListPage.tsx, TaskFilters.tsx                     # single mode
    ├── TaskDetailPage.tsx                                    # no deleted view/Restore; delete 404 handling
    ├── EditTaskPage.tsx                                      # no 409/redirect; save 404 handling
    ├── messages.ts                                           # new texts, remove restore texts
    └── *.test.tsx, test-data.ts                              # updated accordingly

docs/adr/0011-permanent-task-deletion.md                      # new; README table updated
specs/002-task-management/{spec,plan,data-model}.md, contracts/*.md   # "Amended by spec 003" banner
```

**Structure Decision**: The existing four-project backend and the `frontend/` SPA. No new projects or
folders except the migration and the ADR.

## Implementation Notes

- **Order (TDD):**
  1. Domain and Application, starting from failing unit tests.
  2. Infrastructure and the migration, with real-SQLite tests including a soft-deleted row before
     migrating.
  3. API, starting from failing integration tests.
  4. SPA, starting from failing Vitest specs.
  5. Docs.
- **Migration test:** apply all migrations up to `AddTasks`, insert one active and one soft-deleted
  row with raw SQL, apply `RemoveTaskSoftDelete`, then assert. Use `IMigrator.MigrateAsync(target)`
  on a real SQLite connection.
- **Delete statement:** `context.Tasks.Where(t => t.Id == id && t.OwnerId == ownerId).ExecuteDeleteAsync()`.
  It does not go through the change tracker, so nothing is pending in the scope afterwards.
- The **spec 004 kanban** builds on this branch's result once it is merged to `main`.

## Complexity Tracking

No violations.
