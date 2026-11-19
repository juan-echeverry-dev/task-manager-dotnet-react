# Research: Task Management

Phase 0 output for [plan.md](plan.md). The stack is fixed by ADRs 0002–0010 and feature 001, so
no `NEEDS CLARIFICATION` items remain. These decisions settle the design details.

## R1. Naming

- **Decision**: The module is called `Tasks`, the entity `TaskItem` and the status enum
  `TaskItemStatus` (`ToDo`, `InProgress`, `Done`). The API resource is `/api/tasks`.
- **Rationale**: An entity named `Task` would clash with `System.Threading.Tasks.Task`, which every
  async handler uses through implicit usings. Aliases in every file would hurt readability.
- **Alternatives**: `Todo` or `WorkItem` drift from the spec's vocabulary. A `Task` entity with
  `using SysTask = …` aliases is noisy and error-prone.

## R2. Ownership enforcement

- **Decision**:
  - The owner id comes from the `sub` claim through a single `ClaimsPrincipal.GetUserId()`
    extension in `Api/Common`, which is also adopted by the 001 `UsersController`.
  - Every repository method takes the owner id and filters by it inside the query
    (`GetOwnedAsync(id, ownerId)`, `ListAsync(ownerId, query)`).
  - A task owned by someone else is never loaded, so it returns `Error.NotFound` → 404, the same as
    a missing task (FR-002, SC-002).
- **Rationale**: Checking ownership after loading leaks existence through timing and code paths,
  and one forgotten check exposes data.
- **Alternatives**: An EF global query filter on the current user would need a request-scoped
  user in the DbContext, which couples persistence to HTTP; it is also easy to bypass with
  `IgnoreQueryFilters`. 403 for foreign tasks would reveal that the task exists.

## R3. Soft delete

- **Decision**:
  - `TaskItem.DeletedAt` (nullable UTC instant) records deletion; `Delete(now)` and `Restore()`
    are domain methods.
  - Both are idempotent: deleting a deleted task or restoring an active one is a no-op that still
    returns success.
  - `UpdatedAt` is not touched by delete or restore, so a restored task keeps all its values
    (SC-003).
  - `Update(...)` on a deleted task throws, and the handler checks `IsDeleted` first and returns
    `Error.Conflict("Task is deleted", "Restore the task before editing it.")` → 409 (FR-015,
    Clarification).
- **Rationale**: The domain owns the rule "deleted tasks cannot change"; the handler turns it into
  an expected failure instead of an exception.
- **Alternatives**: A separate archive table makes restore and the shared list tooling harder. An
  EF global filter on `DeletedAt` is unnecessary because both lists need explicit control.

## R4. Status values on the wire

- **Decision**: Status travels as a string: `"ToDo"`, `"InProgress"`, or `"Done"`.
  - Request DTOs take `string?`, and the Application validator checks it against the enum names
    (case-sensitive) with the message "Status must be one of: ToDo, InProgress, Done." (FR-008).
  - Responses serialize the enum as a string.
  - The SPA maps the values to the labels "To do", "In progress" and "Done".
- **Rationale**: Readable contract. Validating in Application keeps field errors uniform, like
  feature 001, instead of producing model-binding errors.

## R5. Due date

- **Decision**: `DateOnly?` in the domain, sent as `"yyyy-MM-dd"` and stored as SQLite TEXT in ISO
  format, which sorts correctly as text.
  - **Accepted range:** from 2000-01-01 to 2099-12-31 inclusive (FR-006).
  - **Invalid input** (unparseable or out of range) gets the message "Enter a due date between
    2000-01-01 and 2099-12-31."
  - **Overdue** is computed only in the SPA: `status !== 'Done' && dueDate < localToday`, where
    `localToday` comes from the browser's local calendar date (FR-012). The API knows nothing about
    the person's time zone.
- **Alternatives**: Computing overdue on the server would need the client's time zone on every
  request.

## R6. Timestamps and ordering on SQLite

- **Decision**: `CreatedAt`, `UpdatedAt` and `DeletedAt` are `DateTimeOffset` in the domain. On the
  `Tasks` table they are stored as their UTC tick count (INTEGER) through a small `UtcTicksConverter`.
- **Rationale**: EF Core's SQLite provider cannot `ORDER BY` a `DateTimeOffset` stored as TEXT (ADR
  0004 notes this), and the lists must sort by creation and deletion date. All values are UTC, so
  ticks order correctly and round-trip exactly.
  EF Core's `DateTimeOffsetToBinaryConverter` was the first choice, but it drops precision below
  0.1 ms, so a task read back would differ from the one just returned by create.
- **Alternatives**: Sorting by the v7 `Id` as a creation-time proxy would not work for deletion
  date and is implicit.

## R7. Search

- **Decision**: Search is a case-insensitive "contains" over title and description using SQLite
  `LIKE` with an escape character. The `%`, `_` and escape characters in the term are escaped, the
  term is trimmed, and an empty term means no search.
- **Limitation**: SQLite `LIKE` ignores case only for ASCII letters ("Ñ" ≠ "ñ"). This is accepted
  at demo scale and documented here. The spec already excludes accent folding.
- **Alternatives**: A normalized search column maintained by the domain pushes a persistence
  concern into the entity. Registering a custom SQLite function or full-text search is overkill.

## R8. Sorting and paging

- **Decision**:
  - **Sort fields:** `dueDate`, `createdAt` and `title` for both lists, plus `deletedAt` for the
    deleted list only. Direction is `asc` or `desc`. Without `direction`, each field uses its natural default:
    `dueDate` asc, `title` asc, `createdAt` desc (newest first) and `deletedAt` desc.
  - **Defaults:**
    - Active list: `dueDate asc`. Tasks without a due date always come last whatever the
      direction; ties are broken by `createdAt desc`.
    - Deleted list: `deletedAt desc` (FR-014, FR-017).
  - **Stable paging:** every order ends with `Id` as a final tie-breaker.
  - **Title sort:** case-insensitive using `lower(Title)`, which is ASCII-only, as for search.
  - **Paging:** `page` starts at 1. `pageSize` defaults to 20 and must be between 1 and 100. A page
    beyond the end returns empty `items` with the real `totalCount` (Edge Cases).
  - **Response:** `PagedResult<T>` is `{ items, page, pageSize, totalCount }`.
- **Alternatives**: Keyset pagination performs better at scale, but page numbers are what the spec
  asks for ("move to pages 2 and 3").

## R9. Configurable maximum range

- **Decision**:
  - `TaskListOptions.DueDateRangeMaxDays` lives in Application, defaults to 90, is valid from 1 to
    3650 and is bound from the `Tasks` configuration section with `ValidateOnStart`.
  - The validator rejects a range whose span (`dueTo − dueFrom`, in days) is greater than the
    maximum, and also an inverted range or a range with only one end. Messages:
    - "Choose a range of at most {N} days."
    - "The start date must be on or before the end date."
    - "Choose both a start and an end date."
  - `GET /api/tasks/settings` returns `{ "dueDateRangeMaxDays": N }` so screens use the
    configured value (FR-023).
  - Changing `Tasks__DueDateRangeMaxDays` (environment variable) or `appsettings` takes effect on
    restart, with no release.
- **Rationale**: Application already depends on `Microsoft.Extensions.*` abstractions (logging),
  and `IOptions<T>` is not a framework dependency forbidden by principle III. The SPA must not
  hard-code the limit.
- **Alternatives**: Hard-coding 90 in both tiers contradicts "changeable by configuration".
  Putting the limit in the list response couples it to every list call.

## R10. Update semantics and concurrency

- **Decision**: `PUT /api/tasks/{id}` replaces title, description, status and due date. Sending
  `null` or omitting description or due date clears them, and status is required on update. There
  is no concurrency token: the last write wins, as accepted in the spec. The response is the
  updated task.
- **Alternatives**: `PATCH` with partial updates makes the client form no simpler and adds
  null-versus-absent ambiguity. ETag-based concurrency was rejected by the stakeholder (P7).

## R11. Cross-module reference

- **Decision**:
  - The `Tasks.OwnerId` foreign key references `Users.Id` with `ON DELETE CASCADE` (Assumptions).
    It is configured in `TaskItemConfiguration` through `HasOne<User>()` with no navigation
    properties.
  - Domain and Application of Tasks never reference Identity types.
  - A new ArchUnitNET rule forbids types in the `*.Tasks` namespaces of Domain, Application and Api
    from depending on `*.Identity`. The persistence configuration is exempt, because the database
    is shared (ADR 0003).
- **Rationale**: Real referential integrity at the database level, while the module boundary stays
  enforced in code.

## R12. Demo tasks

- **Decision**:
  - `DemoTasksSeeder` (Infrastructure/Tasks) runs in Development after `SeedDemoAccountAsync`.
  - It looks up the demo account by its configured email and creates six tasks only if that account
    has zero tasks, deleted ones included. Existing tasks are never modified (Clarification).
  - Due dates are relative to the seeding date, so overdue and upcoming tasks always exist:
    - one `ToDo` overdue (today − 3);
    - one `InProgress` due today + 2;
    - one `ToDo` due today + 10;
    - one `Done` with a past date;
    - one `ToDo` without a due date;
    - one `InProgress` due today + 30.
- **Alternatives**: Re-seeding on every start would undo a presenter's changes, which the
  clarification rules out.

## R13. Logging

- **Decision**: There are no task-specific log events. Unexpected errors are still logged by the
  exception handler, and task titles and descriptions are never logged.
- **Rationale**: The spec has no observability requirement for tasks (an Outstanding,
  low-impact item from clarification); logging free text would risk personal data. This can be
  added later through a spec amendment.

## R14. Requirement traceability

| Requirement | Design element | Verified by |
|---|---|---|
| FR-001, FR-002 | Owner from `sub`; owner-scoped repository queries; 404 for foreign tasks | API ownership matrix (get, update, delete, restore × foreign/missing), Infrastructure |
| FR-003–FR-008 | `TaskItem` invariants; Create/Update validators | Unit (domain, validators), API 400 |
| FR-009–FR-011 | Create, Get and Update slices; `PUT` replace | Unit, API, Vitest |
| FR-012 | `overdue.ts` in the SPA | Vitest (today, past, Done, no date) |
| FR-013–FR-015 | `Delete`/`Restore` domain methods; 409 on editing a deleted task; read-only detail | Unit, Infrastructure, API, Vitest |
| FR-016–FR-019, FR-023 | `ListTasks` query and validator; repository search, filters, order, paging; URL state | Unit (validator), Infrastructure (each filter, order, paging), API, Vitest (`task-query.ts`) |
| FR-020–FR-022 | Accessible forms, disabled actions while pending, two empty states | Vitest + manual check |
| SC-004 | Composite indexes; owner-scoped paging | Infrastructure test with 500 tasks (timing sanity check) + manual |
| Demo data | `DemoTasksSeeder` | Infrastructure (once, never re-created, never modified) |
