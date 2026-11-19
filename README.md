# TaskLane

Full-stack application built for a .NET technical exercise: an **ASP.NET Core Web API (.NET 10)**
following **Clean Architecture with vertical slices**, **EF Core + SQLite** persistence, and a
**React + TypeScript** SPA. Development is **spec-driven** ([GitHub Spec Kit](https://github.com/github/spec-kit))
and **test-driven**.

> **Status**: architecture foundation plus user registration and authentication
> ([`specs/001-user-authentication`](specs/001-user-authentication/spec.md)). Business features are
> added through their own specs.

## Documentation

| Document | Content |
|---|---|
| [Constitution](.specify/memory/constitution.md) | Engineering principles and quality gates |
| [Architecture](docs/architecture.md) | Layers, slices, request flow, frontend structure |
| [ADRs](docs/adr/README.md) | Decisions, alternatives, and trade-offs |
| [Development workflow](docs/development-workflow.md) | How to add features with Spec Kit and TDD |
| `specs/` | Feature specifications, plans, and tasks |

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.400+ (pinned in `global.json`) | x64 SDK must come first in `PATH` |
| Node.js | 24+ | npm 11+ |
| PowerShell | 5.1+ or 7+ | For `scripts/test-coverage.ps1` |

No database server or Docker is needed: SQLite runs in-process.

## Getting started

```powershell
git clone https://github.com/juan-echeverry-dev/task-manager-dotnet-react.git
cd task-manager-dotnet-react

# Backend
dotnet tool restore
dotnet build TaskLane.slnx
dotnet run --project src/TaskLane.Api --launch-profile http
#   API:      http://localhost:5208
#   Health:   http://localhost:5208/health
#   API docs: http://localhost:5208/scalar/v1   (Development only)

# Frontend (second terminal)
cd frontend
npm ci
npm run dev
#   SPA:      http://localhost:5173   (proxies /api and /health to the API)
```

In Development the API applies EF Core migrations on startup and creates `tasklane.db` next to
the API project. Delete the file to start from a clean database.

### Demo account

On startup in **Development only**, the API provisions a demo account (idempotent: it is never
duplicated and its password is never reset):

| Email | Password |
|---|---|
| `demo@tasklane.dev` | `TaskLane-Demo-2026` |

The credentials come from the `DemoAccount` section of `src/TaskLane.Api/appsettings.Development.json`.
Other environments never create it.

The demo account also receives **six sample tasks** (every status, one overdue task, one finished
task with a past date, one task without a due date). They are created only while the account has
no tasks at all, deleted ones included, so restarts never duplicate them or undo your changes.

### Tasks

Signed-in people manage their own tasks at `/tasks` (contract:
[`tasks-api.md`](specs/002-task-management/contracts/tasks-api.md); screens:
[`spa-routes.md`](specs/004-task-board/contracts/spa-routes.md)):

- Tasks have a title, an optional description, a status (To do, In progress, Done) and an optional
  due date. Unfinished tasks past their due date are marked **Overdue**.
- The tasks screen is a **board** with one column per status, each with its count and "Load more"
  (spec 004).
- Opening a card shows its details in a **side panel** over the board; creating ("New task" or a
  column's "+") and editing happen in the same panel. Each panel has its own address.
- Deleting, from a card or the panel, asks for confirmation and is permanent (spec 003).
- A filter bar offers search, a due-date range filter and sorting for every column. These values
  live in the address, so a reload or a shared link shows the same board.
- Another person's task always answers "Task not found".

The due-date range filter allows at most **90 days** by default. Change it without a new release
with the `Tasks:DueDateRangeMaxDays` setting (1–3650), for example:

```powershell
$env:Tasks__DueDateRangeMaxDays = 30
dotnet run --project src/TaskLane.Api --launch-profile http
```

The screens read the value from `GET /api/tasks/settings`. An invalid value stops the API at startup.

### Authentication

- `POST /api/auth/register` and `POST /api/auth/sign-in` are anonymous; every other endpoint
  requires `Authorization: Bearer <token>` by default (contract:
  [`identity-api.md`](specs/001-user-authentication/contracts/identity-api.md)).
- Tokens are HS256 JWTs valid for 60 minutes. Development uses the clearly marked key in
  `appsettings.Development.json`; any other environment must provide
  `Authentication__Jwt__SigningKey` (base64, ≥ 32 bytes) through user-secrets or environment
  variables, or the API refuses to start.

## Running tests

```powershell
# All backend tests (unit, infrastructure, API, architecture)
dotnet test --solution TaskLane.slnx

# Backend tests + coverage gate (≥90% Domain+Application, ≥80% overall)
./scripts/test-coverage.ps1
#   HTML report: artifacts/coverage/report/index.html

# Frontend: typecheck, lint, tests, production build
cd frontend
npm run check
```

## Repository layout

```
├─ src/
│  ├─ TaskLane.Domain/            entities, value objects (no dependencies)
│  ├─ TaskLane.Application/       use cases, validation, ports
│  ├─ TaskLane.Infrastructure/    EF Core + SQLite, adapters
│  └─ TaskLane.Api/               HTTP API, composition root
├─ tests/
│  ├─ TaskLane.UnitTests/
│  ├─ TaskLane.Infrastructure.Tests/
│  ├─ TaskLane.Api.Tests/
│  └─ TaskLane.ArchitectureTests/
├─ frontend/                      React + TypeScript SPA (Vite)
├─ docs/                          architecture, ADRs, workflow
├─ specs/                         Spec Kit feature specs
├─ scripts/                       developer scripts (coverage gate)
└─ .specify/  .claude/            Spec Kit configuration and agent skills
```

## Tech stack

.NET 10 · ASP.NET Core · EF Core 10 · SQLite · OpenAPI + Scalar · xUnit v3 · Shouldly ·
ArchUnitNET · React 19 · TypeScript · Vite · React Router · TanStack Query · Tailwind CSS ·
Vitest · Testing Library · GitHub Actions
