# CLAUDE.md

Runtime guidance for AI coding agents working in this repository. The
[constitution](.specify/memory/constitution.md) is authoritative; this file only summarizes it.

## Non-negotiables

- **Spec first**: no business entity, endpoint, or UI flow without an approved spec in
  `specs/NNN-*/`. Use the Spec Kit skills (`/speckit-specify` → `/speckit-clarify` →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`).
  See `docs/development-workflow.md` for how to classify fixes, refactors, and chores.
- **Test first**: Red → Green → Refactor, committed separately (`test:`, `feat:`, `refactor:`).
- **Dependency rule**: Api → Infrastructure → Application → Domain. Application never references
  EF Core or ASP.NET Core. Architecture tests enforce it.
- **No** generic repositories, Unit-of-Work wrappers, MediatR, AutoMapper, or speculative
  interfaces (constitution principle V).
- English for code, comments, commits, and docs. No AI co-author trailers in commits or PRs.
- **Confirm before committing**: present each change (what changed, build and test results) and
  commit only after the repository owner explicitly confirms it. A confirmation covers only the
  changes it names; no commit means no push or PR either.

## Commands

```powershell
dotnet build TaskLane.slnx                       # warnings are errors
dotnet format TaskLane.slnx --verify-no-changes
dotnet test --solution TaskLane.slnx
./scripts/test-coverage.ps1                      # tests + coverage gate
dotnet ef migrations add <Name> --project src/TaskLane.Infrastructure --startup-project src/TaskLane.Api --output-dir Persistence/Migrations
cd frontend; npm run check                       # typecheck, lint, tests, build
```

## Where things go

- Use case: `src/TaskLane.Application/<Module>/<UseCase>/` (command, handler, validator, response)
- Port: `src/TaskLane.Application/<Module>/Ports/`; adapter: `src/TaskLane.Infrastructure/<Module>/`
- EF configuration: `src/TaskLane.Infrastructure/Persistence/Configurations/`
- Controller: `src/TaskLane.Api/<Module>/`
- Frontend feature: `frontend/src/features/<feature>/`; routes in `frontend/src/app/routes.tsx`
- Decisions: `docs/adr/NNNN-title.md`
