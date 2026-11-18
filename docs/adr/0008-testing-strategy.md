# 0008. Testing strategy and tooling

- Status: Accepted
- Date: 2026-11-17

## Context

TDD is mandatory (constitution principle II) and the requirements ask for tests of the data
access layer, the business layer, and the API endpoints, with sufficient coverage.

## Decision

| Project | Scope | Approach |
|---|---|---|
| `TaskLane.UnitTests` | Domain and Application | Fast, isolated; ports replaced by NSubstitute fakes; `TimeProvider` faked |
| `TaskLane.Infrastructure.Tests` | Repositories, EF configuration, adapters | Real SQLite in-memory connection, real migrations |
| `TaskLane.Api.Tests` | HTTP endpoints end to end in-process | `WebApplicationFactory`, isolated SQLite, real auth pipeline |
| `TaskLane.ArchitectureTests` | Dependency rule, framework independence, module boundaries | ArchUnitNET + reflection |
| `frontend` (Vitest) | Components, hooks, API client | Testing Library, `fetch` stubbed |

Tooling: **xUnit v3** on Microsoft.Testing.Platform, **Shouldly** assertions,
**NSubstitute** (added with the first unit tests), **Microsoft.Testing.Extensions.CodeCoverage**
+ **ReportGenerator**.

Coverage gate (`scripts/test-coverage.ps1`, run in CI): ≥ 90% line coverage for
Domain + Application, ≥ 80% overall backend. Generated code and migrations are excluded.

## Alternatives considered

- **FluentAssertions**: commercially licensed since v8.
- **Moq**: viable, but its history with SponsorLink eroded trust; NSubstitute has a simpler API.
- **EF Core InMemory provider**: does not behave like a relational database.
- **Testcontainers**: unnecessary while the production database is SQLite.

## Consequences

- "API endpoint unit tests" are implemented as in-process integration tests: they exercise
  routing, binding, auth, and serialization, which mocked controllers would skip.
- Red → Green → Refactor is visible in history through `test:`, `feat:`, `refactor:` commits.
