# Implementation Plan: Task Management

**Branch**: `002-task-management` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-task-management/spec.md`

## Summary

Add a **Tasks** business module: signed-in people create, view, edit, soft-delete, and restore their
own tasks, and find them in two paginated lists (active and deleted). Both lists offer text search,
a status filter, a due-date range filter with a configurable maximum span (90 days by default),
and a choice of sort order. All list state is kept in the screen address.

The technical approach reuses everything from feature 001:
- **Architecture:** the four-layer Clean Architecture, with one vertical slice per use case.
- **Use cases:** plain handlers returning `Result<T>`, validated with FluentValidation.
- **API:** Problem Details for errors and authentication by default. The owner is always the
  `sub` claim of the session credential.
- **Persistence:** an aggregate-specific `ITaskRepository` on EF Core + SQLite. Ownership is
  enforced in every query, so another person's task is indistinguishable from a missing one.
- **SPA:** a `tasks` feature that keeps list state in the URL. Overdue marking is computed on the
  client against the person's local date.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS; TypeScript 7 (strict) on Node ≥ 24

**Primary Dependencies**: No new packages. The plan reuses ASP.NET Core 10 controllers, EF Core 10
with SQLite, FluentValidation, React 19, React Router 8, TanStack Query 5, React Hook Form + Zod and
Tailwind 4. Application gains a reference to `Microsoft.Extensions.Options`, which is already in the
dependency graph (see [research.md](research.md#r9-configurable-maximum-range)).

**Storage**: SQLite through EF Core. The new `Tasks` table has a foreign key to `Users` (see
[data-model.md](data-model.md)).

**Testing**: The same pyramid as feature 001:
- Unit tests for the domain, validators and handlers, using NSubstitute and FakeTimeProvider.
- Infrastructure tests on real in-memory SQLite: repository queries, ordering, paging and the seeder.
- API tests with `WebApplicationFactory`: contract and ownership matrix.
- ArchUnitNET tests for module boundaries.
- Vitest for the SPA.

**Target Platform**: Local developer machine and GitHub Actions; evergreen desktop and mobile
browsers.

**Project Type**: Web application (ASP.NET Core Web API + React SPA).

**Performance Goals**: SC-004 — with 500 tasks for one person, list queries return in under 1 s
locally. Composite indexes on `(OwnerId, DeletedAt, …)` keep every list query owner-scoped and
indexed.

**Constraints**:
- Ownership is checked in the query itself, never after loading.
- Zero build and console warnings.
- Coverage ≥ 90 % for Domain + Application and ≥ 80 % overall.
- The maximum range is set by configuration with no code change.

**Scale/Scope**: Demo scale (hundreds of tasks per person); 8 endpoints, 1 table, 5 SPA screens.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Pre-design | Post-design |
|---|---|---|---|
| I. Spec-Driven | Approved spec with clarifications; plan traces every FR | ✅ | ✅ Traceability table in [research.md](research.md#r14-requirement-traceability) |
| II. Test-First | Each behavior starts with a failing test; injected clock; real SQLite | ✅ | ✅ Test layers per slice in [quickstart.md](quickstart.md) |
| III. Dependency rule | Application has no EF Core or ASP.NET Core references | ✅ | ✅ Search, paging and ordering are expressed as an Application query object; EF translation lives in Infrastructure |
| IV. Vertical slices and module boundaries | Slices per use case; Tasks does not reach into Identity internals | ✅ | ✅ Tasks refers to people only through the owner id (`sub`). The one cross-module reference (the FK configuration) is in Infrastructure persistence and is exempted explicitly in the new architecture test |
| V. Simplicity | No generic repository, UoW or mediator; interfaces only at ports | ✅ | ✅ One port (`ITaskRepository`) with intention-revealing methods. The shared `PagedResult<T>` is introduced because two lists use it now |
| VI. Security | Authentication by default; ownership enforced; input validated; Problem Details | ✅ | ✅ Every query is filtered by owner, and foreign tasks return 404 like missing ones (SC-002) |
| VII. Quality | Warnings as errors, analyzers, oxlint with zero warnings, English | ✅ | ✅ |

No violations; Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/002-task-management/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0: decisions and rationale
├── data-model.md        # Phase 1: TaskItem entity, Tasks table, validation
├── quickstart.md        # Phase 1: end-to-end validation guide
├── contracts/
│   ├── tasks-api.md     # HTTP contract: /api/tasks/*
│   └── spa-routes.md    # SPA routes, URL state, behaviors
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── TaskLane.Domain/
│   └── Tasks/
│       ├── TaskItem.cs                       # entity: create, update, delete, restore, invariants
│       └── TaskItemStatus.cs                 # ToDo, InProgress, Done
├── TaskLane.Application/
│   ├── Common/Paging/PagedResult.cs          # items + page + pageSize + totalCount
│   └── Tasks/
│       ├── Ports/ITaskRepository.cs          # + TaskListQuery, TaskSortField
│       ├── TaskListOptions.cs                # configurable max due-date range (default 90)
│       ├── TaskResponse.cs                   # shared read model of one task
│       ├── CreateTask/                       # command, validator, handler
│       ├── GetTask/                          # query, handler
│       ├── UpdateTask/                       # command, validator, handler
│       ├── DeleteTask/                       # command, handler
│       ├── RestoreTask/                      # command, handler
│       └── ListTasks/                        # query (active | deleted), validator, handler
├── TaskLane.Infrastructure/
│   ├── Tasks/
│   │   ├── TaskRepository.cs                 # owner-scoped queries, search, sort, paging
│   │   └── DemoTasksSeeder.cs                # Development only, once per demo account
│   └── Persistence/
│       ├── Configurations/TaskItemConfiguration.cs
│       └── Migrations/<timestamp>_AddTasks.cs
└── TaskLane.Api/
    ├── Common/ClaimsPrincipalExtensions.cs   # GetUserId() from "sub" (also used by UsersController)
    └── Tasks/
        ├── TasksController.cs                # 8 endpoints
        └── *Request.cs                       # transport DTOs (nullable fields)

tests/
├── TaskLane.UnitTests/Tasks/                 # TaskItem, validators, handlers
├── TaskLane.Infrastructure.Tests/Tasks/      # repository (ownership, search, filters, order, paging), seeder
├── TaskLane.Api.Tests/Tasks/                 # endpoints, ownership matrix, query validation
└── TaskLane.ArchitectureTests/               # + module boundary: Tasks ↛ Identity

frontend/src/
├── app/routes.tsx                            # + /tasks, /tasks/deleted, /tasks/new, /tasks/:id, /tasks/:id/edit
├── shared/ui/                                # + Pagination, ConfirmDialog, SelectField, TextAreaField
└── features/tasks/
    ├── tasks-api.ts                          # client for the 8 endpoints
    ├── task-query.ts                         # URL <-> list query (defaults, validation)
    ├── overdue.ts                            # isOverdue(task, localToday)
    ├── schemas.ts                            # Zod: task form, range filter
    ├── TaskListPage.tsx                      # active and deleted lists (mode prop)
    ├── TaskFilters.tsx                       # search, status, due range, sort
    ├── TaskForm.tsx                          # create and edit
    ├── NewTaskPage.tsx
    ├── EditTaskPage.tsx
    └── TaskDetailPage.tsx                    # read view; deleted → read-only + Restore
```

**Structure Decision**: Keep the existing layout and add a `Tasks` folder per layer. The entity is
named `TaskItem` to avoid clashing with `System.Threading.Tasks.Task`
([research.md](research.md#r1-naming)).

## Complexity Tracking

No constitution violations to justify.
