# TaskLane

Full-stack application built for a .NET technical exercise: an **ASP.NET Core Web API (.NET 10)**
following **Clean Architecture with vertical slices**, **EF Core + SQLite** persistence, and a
**React + TypeScript** SPA. Development is **spec-driven** ([GitHub Spec Kit](https://github.com/github/spec-kit))
and **test-driven**.

> **Status**: architecture foundation. User registration and authentication are specified next
> (`specs/001-user-authentication`); business features are added through their own specs.

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

**Demo credentials** are seeded by feature 001 and will be listed here.

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
