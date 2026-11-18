# Development workflow

All changes follow the [constitution](../.specify/memory/constitution.md). This page explains
how to apply it day to day.

## 1. Classify the change

| Change | Spec needed? | What is required |
|---|---|---|
| New feature / new behavior | Yes, new `specs/NNN-name/` | Full Spec Kit flow below |
| Change to specified behavior | Yes, amend the existing spec | Update spec (and plan/tasks), then TDD |
| Fix restoring specified behavior | No | Failing regression test first; reference the violated acceptance criterion |
| Fix revealing a gap in the spec | Yes, amend the spec first | Then test, then code |
| Refactor, chore, dependencies, CI, docs | No | Tests stay green; ADR if the decision is significant |

## 2. Spec Kit flow for a feature

Run these commands in Claude Code from the repository root (they are installed as skills in
`.claude/skills/`):

| Step | Command | Output |
|---|---|---|
| Specify | `/speckit-specify <feature description>` | Branch `NNN-short-name` and `specs/NNN-short-name/spec.md` (user stories, acceptance criteria, edge cases; no technology) |
| Clarify | `/speckit-clarify` | Ambiguities resolved and recorded in the spec |
| Plan | `/speckit-plan` | `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`; must pass the Constitution Check |
| Tasks | `/speckit-tasks` | `tasks.md`: ordered, test-first tasks per user story |
| Analyze | `/speckit-analyze` | Consistency report across spec, plan, and tasks (fix issues before implementing) |
| Implement | `/speckit-implement` | Code, built task by task with Red → Green → Refactor |

Optional: `/speckit-checklist` for requirement-quality checklists, `/speckit-converge` to append
missing work when the code diverges from the plan.

## 3. Test-driven implementation

For every behavior in `tasks.md`:

1. **Red**: write a failing test that expresses the acceptance criterion. Commit `test: ...`.
2. **Green**: write the minimum production code to pass. Commit `feat: ...` (or `fix: ...`).
3. **Refactor**: improve design with tests green. Commit `refactor: ...`.

Choose the test level by what is being specified: Domain/Application rules → `UnitTests`;
persistence → `Infrastructure.Tests`; HTTP contract → `Api.Tests`; UI behavior → Vitest.

## 4. Adding a business module (example shape)

When a spec introduces a new business entity, the plan typically adds:

- `Domain/<Module>/` entity and value objects with their invariants (unit tested).
- `Application/<Module>/Ports/I<Entity>Repository.cs` and one folder per use case.
- `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`, the repository adapter,
  and a migration: `dotnet ef migrations add <Name> --project src/TaskLane.Infrastructure --startup-project src/TaskLane.Api --output-dir Persistence/Migrations`.
- `Api/<Module>/<Entity>Controller.cs` returning Problem Details on failures.
- An architecture test asserting the module does not depend on other modules' internals.
- `frontend/src/features/<module>/` with its routes registered in `src/app/routes.tsx`.

## 5. Before opening a pull request

```powershell
dotnet build TaskLane.slnx                     # zero warnings (warnings are errors)
dotnet format TaskLane.slnx --verify-no-changes
./scripts/test-coverage.ps1                    # all backend tests + coverage gate
cd frontend; npm run check                     # typecheck, lint, tests, build
```

CI runs the same gates on every pull request. Commits follow Conventional Commits; branches are
named after the spec (`NNN-short-name`) or `chore/<topic>` for non-feature work.
