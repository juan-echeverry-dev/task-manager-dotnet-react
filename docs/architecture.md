# Architecture

TaskLane is a full-stack application: an ASP.NET Core Web API (.NET 10) following Clean
Architecture with vertical slices, a SQLite database accessed through EF Core, and a React SPA.
Decisions and trade-offs are recorded in [ADRs](adr/README.md); engineering principles in the
[constitution](../.specify/memory/constitution.md).

## System context

```mermaid
flowchart LR
    user([User]) --> spa[React SPA<br/>Vite dev server :5173]
    spa -- "/api, /health (proxied, same origin)" --> api[TaskLane.Api<br/>ASP.NET Core :5208]
    api --> db[(SQLite<br/>tasklane.db)]
```

## Layers and dependency rule

```mermaid
flowchart TB
    Api[TaskLane.Api<br/>controllers, auth policies, ProblemDetails,<br/>composition root] --> Infrastructure
    Api --> Application
    Infrastructure[TaskLane.Infrastructure<br/>EF Core + SQLite, hashing, JWT issuing] --> Application
    Application[TaskLane.Application<br/>use cases, validation, ports] --> Domain
    Domain[TaskLane.Domain<br/>entities, value objects, invariants]
```

Dependencies point inward only. This is enforced by the project graph (an outward reference is a
circular dependency) and by `TaskLane.ArchitectureTests`, which also forbid EF Core, SQLite, and
ASP.NET Core assemblies in Domain and Application.

## Vertical slices inside the layers

Each layer is organized by **module** and then by **use case**. A use case keeps everything it
needs together; slices never call each other.

```
src/
├─ TaskLane.Domain/
│  └─ <Module>/                 entities and value objects of the module
├─ TaskLane.Application/
│  └─ <Module>/
│     ├─ Ports/                 interfaces the module needs from the outside
│     └─ <UseCase>/             Command/Query, Handler, Validator, Response
├─ TaskLane.Infrastructure/
│  ├─ Persistence/              AppDbContext, Configurations/, Migrations/
│  └─ <Module>/                 adapters implementing the module's ports
└─ TaskLane.Api/
   └─ <Module>/                 controller(s) and HTTP contracts for the module
```

Request flow for a use case:

```mermaid
sequenceDiagram
    participant C as Controller (Api)
    participant H as Handler (Application)
    participant V as Validator (Application)
    participant D as Domain
    participant P as Port → Adapter (Infrastructure)
    C->>H: command
    H->>V: validate
    V-->>H: errors? → Result.Failure
    H->>D: apply business rules
    H->>P: persist / query
    H-->>C: Result<T>
    C-->>C: map Result → 2xx or ProblemDetails
```

## Modules

| Module | Purpose | Status |
|---|---|---|
| Identity | Registration, sign-in, JWT issuing, current-user profile, demo account | Implemented by [`specs/001-user-authentication`](../specs/001-user-authentication/spec.md) |
| Tasks | Create, view, edit and permanently delete one's own tasks; search, filters, sort and paging | Implemented by [`specs/002-task-management`](../specs/002-task-management/spec.md), amended by [`specs/003-permanent-task-deletion`](../specs/003-permanent-task-deletion/spec.md) |

### Identity module

| Layer | Contents |
|---|---|
| Domain | `Identity/User` — registration invariants and the single email normalization rule |
| Application | Ports `IUserRepository`, `IPasswordHasher`, `IAccessTokenIssuer`; slices `Register`, `SignIn`, `GetProfile`; `IdentityLog` (auth events, no PII) |
| Infrastructure | `UserRepository` (EF Core; unique-index violation → `false`), `PasswordHasherAdapter` (`PasswordHasher<T>`), `JwtAccessTokenIssuer`, `JwtOptions`, `DemoAccountSeeder` |
| Api | `AuthController` (`POST /api/auth/register`, `POST /api/auth/sign-in`, anonymous), `UsersController` (`GET /api/users/me`), `AuthenticationSetup` |

Authentication pipeline:

```mermaid
sequenceDiagram
    participant SPA
    participant Auth as AuthController
    participant SignIn as SignInHandler
    participant Users as UsersController
    participant Bearer as JwtBearer + fallback policy
    SPA->>Auth: POST /api/auth/sign-in {email, password}
    Auth->>SignIn: SignInCommand
    SignIn-->>Auth: token (sub, jti, 60 min) or uniform 401
    Auth-->>SPA: 200 {accessToken, expiresAt}
    SPA->>Bearer: GET /api/users/me (Authorization: Bearer)
    Bearer-->>SPA: 401 if missing / expired / tampered / wrong issuer or audience
    Bearer->>Users: authenticated principal (sub)
    Users-->>SPA: profile, or 401 if the account no longer exists
```

The SPA keeps `{accessToken, expiresAt}` in `sessionStorage` (per tab). `RequireAuth` guards
protected routes and remembers a safe `returnTo`; any 401 ends the session and shows the expiry
message when `expiresAt` has passed.

Modules interact only through public contracts (e.g. the authenticated user id claim). See
[ADR 0003](adr/0003-identity-module-in-a-single-host.md).

### Tasks module

| Layer | Contents |
|---|---|
| Domain | `Tasks/TaskItem` (named to avoid `System.Threading.Tasks.Task`) and `TaskItemStatus`: invariants and any status transition |
| Application | Port `ITaskRepository` with `TaskListQuery`; slices `CreateTask`, `GetTask`, `UpdateTask`, `DeleteTask` (permanent), `ListTasks`; `TaskFieldRules` shared by the create and edit validators; `TaskListOptions` (configurable range limit) |
| Infrastructure | `TaskRepository` (owner-scoped queries and delete, `LIKE` search with escaping, filters, sort, paging), `TaskItemConfiguration` (FK to `Users` with cascade, UTC-tick timestamps, owner indexes), `DemoTasksSeeder` |
| Api | `TasksController`: `GET/POST /api/tasks`, `GET /api/tasks/settings`, `GET/PUT/DELETE /api/tasks/{id}` ([contract](../specs/002-task-management/contracts/tasks-api.md), [changes in 003](../specs/003-permanent-task-deletion/contracts/tasks-api.md)) |

**Module boundary**: Tasks knows a person only by the owner id taken from the `sub` claim
(`ClaimsPrincipalExtensions.GetUserId`). `ModuleBoundaryTests` fails the build if Tasks types in
Domain, Application or Api depend on Identity types. The only exception is the persistence
configuration, which declares the foreign key from `Tasks.OwnerId` to `Users.Id`.

**Ownership**: every repository query starts from the owner's tasks, so another person's task is
indistinguishable from a missing one (the same 404 body). Deletion is permanent (spec 003); editing
or deleting a task that no longer exists returns the same 404.

List query flow:

```mermaid
sequenceDiagram
    participant SPA as Board column (useBoardColumn)
    participant C as TasksController
    participant V as ListTasksValidator
    participant H as ListTasksHandler
    participant R as TaskRepository
    SPA->>C: GET /api/tasks?status=…&pageSize=20&search=…&dueFrom=…&dueTo=…&sort=…&direction=…&page=…
    C->>H: ListTasksQuery (raw text values, owner from sub)
    H->>V: validate (range limit from TaskListOptions)
    V-->>H: errors keyed by parameter → 400
    H->>R: TaskListQuery (parsed, defaults applied)
    R-->>H: page of the owner's tasks + total count
    H-->>SPA: 200 { items, page, pageSize, totalCount }
```

## Cross-cutting concerns

| Concern | Approach |
|---|---|
| Errors | RFC 9457 Problem Details for every error response ([ADR 0010](adr/0010-http-api-conventions.md)) |
| Expected failures | `Result`/`Error` values from handlers, mapped to HTTP in the Api ([ADR 0007](adr/0007-use-cases-without-mediator.md)) |
| Validation | FluentValidation in Application; invariants in Domain |
| Authentication | JWT bearer, authenticated-by-default fallback policy ([ADR 0006](adr/0006-jwt-bearer-authentication.md)) |
| Time | `TimeProvider` injected; never `DateTime.UtcNow` in business code |
| Configuration | `appsettings*.json` + user-secrets / environment variables for secrets; validated on start (`ValidateOnStart`) |
| Health | `GET /health` including a database check |
| API docs | OpenAPI + Scalar in Development |

## Frontend structure

```
frontend/src/
├─ app/            composition root: AppProviders (session, query client, router), route table, pages
├─ features/       one folder per feature (components, hooks, API calls, tests)
├─ shared/         API client (ProblemDetails → ApiError), layout, UI primitives
└─ test/           test setup and render helpers
```

Server state is managed by TanStack Query; there is no global client store. The only client
state shared across screens is the session (`features/auth/session`). The task board keeps its
search, due-date range and sort in the address (`features/tasks/board-query.ts`), so reloading or
sharing a link shows the same board; each column loads its own batches, and task details, create and
edit open in a side panel routed under `/tasks` ([ADR 0012](adr/0012-task-board-routed-side-panel.md)). Any 401, from a query or a mutation, ends the session. See
[ADR 0009](adr/0009-frontend-stack.md).
