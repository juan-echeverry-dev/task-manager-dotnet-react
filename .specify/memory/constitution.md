# .NET Full-Stack Exercise Constitution

## Core Principles

### I. Spec-Driven Development (NON-NEGOTIABLE)

Every change in observable behavior MUST flow through the Spec Kit workflow:
`/speckit-specify` → `/speckit-clarify` (when ambiguity exists) → `/speckit-plan` →
`/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`.

- Business entities, endpoints, and UI flows MUST NOT exist without an approved spec in
  `specs/<NNN-feature>/`. No placeholder or speculative domain code.
- Specs describe *what* and *why* (user value, acceptance criteria, edge cases) and stay
  technology-agnostic; technical choices live in `plan.md` and ADRs.
- Infrastructure-only work (scaffolding, CI, tooling) MAY skip the spec but MUST be traceable to
  a plan or an ADR.

Change classification:

- **Features** and any change to specified behavior MUST create or amend a spec.
- **Fixes** restoring already-specified behavior need no new spec but MUST start with a failing
  regression test and reference the violated acceptance criterion. If the bug exposes a gap in
  the spec, the spec MUST be amended first.
- **Refactors, chores, dependency updates, CI, and docs** need no spec; existing tests MUST stay
  green, and significant decisions require an ADR.

**Rationale**: specs are the contract between intent and code; they make AI-assisted generation
reviewable and keep scope explicit.

### II. Test-First Development (NON-NEGOTIABLE)

Production code that implements behavior MUST be preceded by a failing test.

- Red → Green → Refactor is mandatory. Each step SHOULD be an atomic commit (`test:`, `feat:`,
  `refactor:`) so the cycle is visible in history.
- Exempt from test-first: scaffolding, configuration, EF Core migrations, CI, and documentation.
- Test pyramid: domain and application rules are covered by fast unit tests; persistence by tests
  against a real SQLite database (no mocked `DbContext`); HTTP endpoints by in-process
  integration tests (`WebApplicationFactory`); dependency rules by architecture tests.
- Tests MUST be deterministic: no shared mutable state, no real clock (inject `TimeProvider`),
  no network, no reliance on execution order.
- Tests assert behavior through public contracts, not implementation details.

**Rationale**: TDD drives design toward testable seams and is an explicit evaluation criterion.

### III. Clean Architecture Dependency Rule

Source dependencies MUST point inward only: `Api → Infrastructure → Application → Domain`, with
`Api` acting as the composition root.

- `Domain` has zero dependencies on frameworks, persistence, or HTTP.
- `Application` (business logic and validation) depends only on `Domain` and on abstractions it
  owns (ports). It MUST NOT reference EF Core, ASP.NET Core, or any infrastructure package.
- `Infrastructure` implements Application ports (persistence, hashing, token issuing, clock).
- `Api` handles transport concerns only: routing, model binding, auth policies, mapping results
  to HTTP. It contains no business rules.
- These rules MUST be enforced by automated architecture tests that fail the build on violation.

**Rationale**: business rules stay independent of the data layer and the API, as required, and
infrastructure stays replaceable.

### IV. Vertical Slices Inside the Layers

Code is organized by feature (use case), not by technical type, within each layer.

- Each module (e.g. `Identity`) owns a folder per layer; each use case is a slice containing its
  request, handler, validator, and response, kept together.
- Slices MUST NOT call other slices directly. Shared behavior lives in the Domain or in an
  explicitly shared Application abstraction.
- Modules MUST NOT reference each other's internals; cross-module interaction uses only public
  contracts (e.g. the authenticated user id claim). Module boundaries are covered by
  architecture tests.
- Extract shared code only after a real third occurrence (rule of three).

**Rationale**: high cohesion per feature, low coupling between features, and new features are
additive instead of scattered across layers.

### V. Simplicity and Justified Abstractions

Prefer the simplest design that satisfies the spec (YAGNI, KISS).

- Forbidden unless justified in the plan's Complexity Tracking table: generic repositories,
  Unit-of-Work wrappers over EF Core, mediator libraries (MediatR or equivalents), AutoMapper,
  service locators, and abstract base classes with a single implementation.
- An interface is created only at an architectural boundary (port) or when it is a required test
  seam; not "just in case".
- New third-party dependencies require a stated reason, an OSI-approved license, and active
  maintenance.

**Rationale**: every abstraction has a reading and maintenance cost; it must pay for itself.

### VI. Security by Default

- Passwords MUST be stored only as salted, adaptive hashes produced by a vetted library
  implementation; custom cryptography is forbidden. Plain-text passwords are never logged,
  persisted, or returned.
- Endpoints require authentication by default (fallback authorization policy); anonymous access
  MUST be explicit and minimal.
- JWTs MUST be signed, validated for issuer, audience, lifetime, and signature, and carry no
  sensitive data. Signing keys and secrets MUST NOT be committed; development uses user-secrets
  or environment variables, with a clearly marked dev-only fallback.
- All external input is validated at the application boundary. Errors are returned as RFC 9457
  Problem Details without stack traces or internal details.
- Authentication failures use uniform messages that do not reveal whether an account exists.
- Email identifiers are normalized and unique; uniqueness is enforced by the database as well.

**Rationale**: user registration and authentication are explicit requirements; mistakes there
are the costliest to fix.

### VII. Code Quality and Maintainability

- SOLID applied pragmatically; small, cohesive units with intention-revealing names.
- Backend: nullable reference types enabled, warnings treated as errors, .NET analyzers on,
  formatting enforced via `.editorconfig` and `dotnet format`.
- Frontend: TypeScript strict mode, ESLint with zero warnings, no warnings or errors in the
  browser console during normal use.
- Code, identifiers, comments, commit messages, and documentation are written in English.
- Significant architectural decisions are recorded as ADRs in `docs/adr/`.

**Rationale**: the code base is reviewed live; readability and consistency are evaluated as much
as functionality.

## Technical Constraints

- Backend: C# on the current .NET LTS, ASP.NET Core Web API (controllers).
- Persistence: relational database accessed through EF Core with versioned migrations; the
  application MUST run locally without external services or containers.
- Frontend: a SPA in TypeScript communicating with the API over HTTP/JSON.
- Only dependencies with free, OSI-approved licenses (commercially licensed packages such as
  MediatR ≥ 13 or FluentAssertions ≥ 8 are excluded).
- A fresh clone MUST build, test, and run following the README alone, including seeded demo
  credentials.

Concrete technology choices and their trade-offs are recorded in ADRs, not here.

## Development Workflow and Quality Gates

- Work happens on feature branches named after the spec (`NNN-short-name`) and merges to `main`
  through a pull request.
- Commits follow Conventional Commits. No AI co-author trailers.
- A change is mergeable only when all of the following pass, locally and in CI:
  1. `dotnet build` with zero warnings and `dotnet format --verify-no-changes`.
  2. All backend test projects, including architecture tests.
  3. Coverage: ≥ 90% line coverage for Domain + Application, ≥ 80% overall backend.
  4. Frontend type-check, lint (zero warnings), tests, and production build.
- Every `plan.md` MUST pass the Constitution Check; any deviation is recorded in its Complexity
  Tracking table with the simpler alternative that was rejected and why.

## Governance

This constitution supersedes any other practice in the repository. When guidance conflicts, the
constitution wins until amended.

- Amendments are proposed via pull request that updates this file, includes a Sync Impact
  Report, and bumps the version using semantic versioning: MAJOR for removing or redefining a
  principle, MINOR for adding a principle or materially expanding guidance, PATCH for wording.
- Reviews of every spec, plan, and pull request MUST verify compliance with these principles.
- Runtime guidance for AI agents lives in `CLAUDE.md` and MUST stay consistent with this file.

**Version**: 1.0.0 | **Ratified**: 2026-11-17 | **Last Amended**: 2026-11-17
