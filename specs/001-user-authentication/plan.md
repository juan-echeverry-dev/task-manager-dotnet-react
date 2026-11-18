# Implementation Plan: User Registration and Authentication

**Branch**: `001-user-authentication` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-user-authentication/spec.md`

## Summary

Add the **Identity module**: account registration, sign-in that issues a 60-minute signed access
token, a protected profile endpoint, authentication required by default for every endpoint, an
idempotent demo account in Development, and the matching SPA screens (register, sign-in,
profile, sign-out, session expiry, safe return-to-original-screen).

Technical approach: the existing four-project Clean Architecture with one vertical slice per use
case (`Register`, `SignIn`, `GetProfile`). Handlers are plain classes returning `Result<T>`
(ADR 0007), validated with FluentValidation, persisting through an aggregate-specific
`IUserRepository` on EF Core + SQLite (ADR 0004), hashing with `PasswordHasher<T>` (ADR 0005),
issuing and validating JWTs (ADR 0006). The SPA keeps the token in `sessionStorage` behind a
session context and guards routes on the client; the API is the source of truth.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS (SDK 10.0.400); TypeScript 7 (strict) on Node ≥ 24

**Primary Dependencies**: ASP.NET Core 10 (controllers), EF Core 10 (SQLite provider),
`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core`
(`PasswordHasher<T>`), FluentValidation; React 19, React Router 8, TanStack Query 5,
React Hook Form + Zod, Tailwind 4. New packages and the reasons are listed in
[research.md](research.md#r10-new-dependencies).

**Storage**: SQLite file via EF Core with versioned migrations; one new table `Users`
([data-model.md](data-model.md)).

**Testing**: xUnit v3 (Microsoft.Testing.Platform) + Shouldly + NSubstitute; FakeTimeProvider and
FakeLogger from Microsoft's testing extensions; real in-memory SQLite for persistence and API
tests; ArchUnitNET; Vitest + Testing Library for the SPA.

**Target Platform**: Local developer machine (Windows/macOS/Linux) and GitHub Actions
(ubuntu-latest); evergreen desktop and mobile browsers.

**Project Type**: Web application (ASP.NET Core Web API + React SPA in `frontend/`).

**Performance Goals**: Interactive use only; no throughput target. Sign-in cost is dominated by
password hashing (PBKDF2, ~100k iterations), which is intentional. SC-001 (account + sign-in in
≤ 90 s) is a UX target, not a latency budget.

**Constraints**: No external services or containers; secrets never committed (dev-only key
clearly marked); zero build warnings; zero browser console warnings; coverage ≥ 90 %
Domain + Application and ≥ 80 % overall.

**Scale/Scope**: Demo scale (tens of accounts); 3 endpoints, 1 table, 3 SPA screens.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Pre-design | Post-design |
|---|---|---|---|
| I. Spec-Driven | Approved spec with clarifications exists; plan traces every FR | ✅ | ✅ FR→design map in [research.md](research.md#r12-requirement-traceability) |
| II. Test-First | Each behavior starts with a failing test; `TimeProvider` injected; real SQLite | ✅ | ✅ Test layers per slice defined in [quickstart.md](quickstart.md) |
| III. Dependency rule | Application has no EF Core / ASP.NET Core references | ✅ | ✅ JWT bearer, EF and hasher live in Api/Infrastructure; Application only uses `Microsoft.Extensions.Logging.Abstractions` and FluentValidation |
| IV. Vertical slices | One folder per use case per layer; no slice-to-slice calls | ✅ | ✅ `Register`, `SignIn`, `GetProfile` slices; shared `Result` in `Application/Common` |
| V. Simplicity | No generic repo, UoW, mediator, AutoMapper; interfaces only at ports | ✅ | ✅ Three ports (`IUserRepository`, `IPasswordHasher`, `IAccessTokenIssuer`), all at architectural boundaries; no `ICurrentUser` (controller passes the id) |
| VI. Security | Salted adaptive hash; fallback auth policy; JWT fully validated; no secrets committed; uniform auth failures; DB-enforced unique email | ✅ | ✅ Plus timing-equalized failed sign-in (R4) and log redaction tests (SC-007) |
| VII. Quality | Warnings as errors, analyzers, oxlint zero warnings, English | ✅ | ✅ `LoggerMessage` source-generated logging satisfies CA1848 |

No violations: Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-user-authentication/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0: decisions and rationale
├── data-model.md        # Phase 1: User entity, table, validation
├── quickstart.md        # Phase 1: how to validate the feature end to end
├── contracts/
│   ├── identity-api.md  # HTTP contract: /api/auth/*, /api/users/me
│   └── spa-routes.md    # SPA routes, guards, messages
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── TaskLane.Domain/
│   └── Identity/
│       └── User.cs                         # entity + invariants + email normalization
├── TaskLane.Application/
│   ├── Common/Results/                     # Result, Result<T>, Error, ErrorType
│   └── Identity/
│       ├── Ports/                          # IUserRepository, IPasswordHasher, IAccessTokenIssuer
│       ├── IdentityLog.cs                  # source-generated auth event logging (FR-019)
│       ├── Register/                       # RegisterUserCommand, Validator, Handler, Response
│       ├── SignIn/                         # SignInCommand, Validator, Handler, Response
│       └── GetProfile/                     # GetProfileQuery, Handler, ProfileResponse
├── TaskLane.Infrastructure/
│   ├── Identity/
│   │   ├── UserRepository.cs               # EF Core; unique-violation → false
│   │   ├── PasswordHasherAdapter.cs        # PasswordHasher<User>
│   │   ├── JwtAccessTokenIssuer.cs         # JsonWebTokenHandler, HS256
│   │   ├── JwtOptions.cs                   # bound + validated on start
│   │   └── DemoAccountSeeder.cs            # Development only, idempotent
│   └── Persistence/
│       ├── Configurations/UserConfiguration.cs
│       └── Migrations/<timestamp>_AddUsers.cs
└── TaskLane.Api/
    ├── Common/ResultMappings.cs            # Result → ProblemDetails / IActionResult
    ├── Identity/
    │   ├── AuthController.cs               # POST register, POST sign-in (anonymous)
    │   ├── UsersController.cs              # GET me (authenticated)
    │   └── AuthenticationSetup.cs          # JwtBearer + fallback policy
    └── Program.cs

tests/
├── TaskLane.UnitTests/Identity/            # User, validators, handlers (fakes + FakeTimeProvider)
├── TaskLane.Infrastructure.Tests/Identity/ # repository, hasher, token issuer, seeder, migration
├── TaskLane.Api.Tests/Identity/            # endpoints, auth pipeline, log redaction
└── TaskLane.ArchitectureTests/             # existing rules + Api controllers have no business deps

frontend/src/
├── app/routes.tsx                          # + /register, /sign-in, /profile
├── shared/api/http-client.ts               # + optional bearer token
├── features/auth/
│   ├── session/                            # SessionProvider, useSession, session-storage
│   ├── auth-api.ts                         # register, signIn
│   ├── RegisterPage.tsx
│   ├── SignInPage.tsx
│   ├── RequireAuth.tsx                     # guard + returnTo
│   ├── RedirectIfAuthenticated.tsx
│   └── return-path.ts                      # open-redirect guard
└── features/profile/
    ├── profile-api.ts
    └── ProfilePage.tsx
```

**Structure Decision**: Keep the existing layout (`src/`, `tests/`, `frontend/`) and add an
`Identity` folder per backend layer, with one sub-folder per use case. On the frontend,
`features/auth` holds the session and the anonymous screens, and `features/profile` holds the
protected screen.

## Complexity Tracking

No constitution violations to justify.
