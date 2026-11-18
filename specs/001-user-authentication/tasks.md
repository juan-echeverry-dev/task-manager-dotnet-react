---

description: "Task list for feature 001 — User Registration and Authentication"
---

# Tasks: User Registration and Authentication

**Input**: Design documents from `specs/001-user-authentication/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: REQUIRED. Constitution principle II (Test-First, NON-NEGOTIABLE) applies. Every test task is
written first and MUST fail (Red) before the implementation task that follows it (Green), and each
cycle ends with an optional refactor. Commit each step separately: `test:`, then `feat:`, then
`refactor:`.

**Organization**: Tasks are grouped by user story, so each story can be implemented and verified
on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: User story from spec.md (US1–US4)
- Paths are relative to the repository root: backend in `src/` and `tests/`, SPA in `frontend/src/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependencies and configuration needed before any test can be written.

- [X] T001 Add package versions to `Directory.Packages.props`:
  - **Runtime:** FluentValidation 12.2.0; Microsoft.Extensions.Logging.Abstractions, Microsoft.Extensions.Identity.Core and Microsoft.AspNetCore.Authentication.JwtBearer at `$(DotNetPackagesVersion)`; Microsoft.IdentityModel.JsonWebTokens at the version JwtBearer 10.0.15 depends on.
  - **Testing:** NSubstitute 6.2.0; Microsoft.Extensions.TimeProvider.Testing 10.11.0; Microsoft.Extensions.Diagnostics.Testing 10.11.0.
- [X] T002 Reference the packages from their projects:
  - FluentValidation and Logging.Abstractions in `src/TaskLane.Application/TaskLane.Application.csproj`.
  - Identity.Core and JsonWebTokens in `src/TaskLane.Infrastructure/TaskLane.Infrastructure.csproj`.
  - JwtBearer in `src/TaskLane.Api/TaskLane.Api.csproj`.
  - NSubstitute, TimeProvider.Testing and Diagnostics.Testing in `tests/TaskLane.UnitTests/TaskLane.UnitTests.csproj`.
  - TimeProvider.Testing in `tests/TaskLane.Infrastructure.Tests/TaskLane.Infrastructure.Tests.csproj`.
  - TimeProvider.Testing and Diagnostics.Testing in `tests/TaskLane.Api.Tests/TaskLane.Api.Tests.csproj`.
  - Then run `dotnet build TaskLane.slnx` (0 warnings) and `dotnet test --solution TaskLane.slnx` (green).
- [X] T003 [P] Install the SPA form libraries with `npm install react-hook-form zod @hookform/resolvers` in `frontend/package.json`, then run `npm run check` (green).
- [X] T004 [P] Register `TimeProvider.System` as a singleton in `src/TaskLane.Application/DependencyInjection.cs`. This is configuration only and is exempt from test-first.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared result types, the `User` entity and table, and the authentication-by-default pipeline. Every story depends on them.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [X] T005 [P] Write failing unit tests for `Result`, `Result<T>` and `Error`, covering:
  - success and failure;
  - accessing `Value` on a failure throws;
  - the factories `Error.Validation(IReadOnlyDictionary<string, string[]>)`, `Error.Conflict(title, detail)`, `Error.Unauthorized(title, detail)` and `Error.NotFound(title, detail)` expose `ErrorType`.

  File: `tests/TaskLane.UnitTests/Common/ResultTests.cs`
- [X] T006 Implement `Result`, `Result<T>`, `Error` and `ErrorType` in `src/TaskLane.Application/Common/Results/` (one file per type) until T005 passes. Then remove the temporary `--ignore-exit-code 8` block from `tests/TaskLane.UnitTests/TaskLane.UnitTests.csproj`.
- [X] T007 [P] Write failing unit tests for `User.Register(id, email, displayName, passwordHash, createdAt)` and `User.NormalizeEmail`:
  - `Email` is trimmed; `NormalizedEmail` is the trimmed email lower-cased (invariant culture).
  - `DisplayName` is trimmed.
  - `ArgumentException` is thrown for an empty or whitespace email/displayName/passwordHash, an email over 254 chars after trim, a displayName over 100 chars after trim, and a non-UTC `createdAt` (offset ≠ 0).

  File: `tests/TaskLane.UnitTests/Identity/UserTests.cs`
- [X] T008 Implement the sealed entity `User` in `src/TaskLane.Domain/Identity/User.cs` until T007 passes:
  - Properties `Id` (Guid), `Email`, `NormalizedEmail`, `DisplayName`, `PasswordHash`, `CreatedAt` (DateTimeOffset), all with private setters.
  - A private parameterless constructor for EF Core.
  - Public constants `EmailMaxLength = 254` and `DisplayNameMaxLength = 100`.
- [X] T009 [P] Write a failing Infrastructure test that applies migrations and checks the `Users` table:
  - Saving two users whose `NormalizedEmail` is equal throws `DbUpdateException`.
  - A saved user round-trips all six fields unchanged.

  Use an in-memory SQLite connection like `DatabaseInitializerTests`. File: `tests/TaskLane.Infrastructure.Tests/Identity/UserPersistenceTests.cs`
- [X] T010 Create `UserConfiguration` in `src/TaskLane.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, then add the migration with `dotnet ef migrations add AddUsers --project src/TaskLane.Infrastructure --startup-project src/TaskLane.Api --output-dir Persistence/Migrations`. T009 must then pass. Configuration:
  - Table `Users`, key `Id`.
  - `Email` "NOT NULL, max 254".
  - `NormalizedEmail` "NOT NULL, max 254, unique index `IX_Users_NormalizedEmail`".
  - `DisplayName` "NOT NULL, max 100".
  - `PasswordHash` NOT NULL.
  - `CreatedAt` NOT NULL.
- [X] T011 Rework the API test host so it has a real schema (research R11), keeping the existing `PlatformEndpointsTests` green:
  - `tests/TaskLane.Api.Tests/ApiFactory.cs` keeps one open in-memory `SqliteConnection` per factory and replaces the `DbContextOptions<AppDbContext>` registration to use it.
  - It applies migrations on host start and disposes the connection with the factory.
  - It sets `Authentication:Jwt:*` test settings: issuer `tasklane-tests`, audience `tasklane-tests`, and a random 32-byte base64 signing key per factory.
  - It registers a `FakeTimeProvider` and `FakeLogCollector` (`AddFakeLogging`), exposed as properties.
- [X] T012 [P] Write failing API tests:
  - A test-only endpoint without `[AllowAnonymous]` returns 401 with Problem Details. Add it via a test controller in `tests/TaskLane.Api.Tests/Support/ProtectedProbeController.cs`, registered with `AddApplicationPart`.
  - `GET /health` still returns 200 anonymously.
  - An unexpected exception (test-only anonymous endpoint that throws, in `tests/TaskLane.Api.Tests/Support/ThrowingProbeController.cs`) returns 500 `application/problem+json`. The body contains no `exception`, `stackTrace`, exception message or connection string (FR-015).
  - File: `tests/TaskLane.Api.Tests/AuthenticationByDefaultTests.cs` (the 500 case goes in `tests/TaskLane.Api.Tests/ErrorHandlingTests.cs`)
- [X] T013 [P] Add `JwtOptions` in `src/TaskLane.Infrastructure/Identity/JwtOptions.cs`:
  - Section `Authentication:Jwt`, with `Issuer`, `Audience`, `SigningKey` (base64, ≥ 32 bytes decoded) and `LifetimeMinutes` (default 60, range 1–1440).
  - Bind it in `src/TaskLane.Infrastructure/DependencyInjection.cs` with validation and `ValidateOnStart`.
  - Add a clearly marked dev-only key (comment `"_comment": "DEVELOPMENT ONLY — never use outside local development"`) to `src/TaskLane.Api/appsettings.Development.json`.
  - Cover missing or short keys with a failing-first test in `tests/TaskLane.Infrastructure.Tests/DependencyInjectionTests.cs`.
- [X] T014 Implement authentication by default until T012 passes. Depends on T013 (`JwtOptions`).
  - `src/TaskLane.Api/Identity/AuthenticationSetup.cs` adds `AddJwtBearer`. It validates issuer, audience, lifetime, signing key and algorithm HS256, uses `ClockSkew = TimeSpan.Zero`, `MapInboundClaims = false`, and the DI `TimeProvider`.
  - It reads `JwtOptions` (T013) and sets the fallback policy `RequireAuthenticatedUser`.
  - In `src/TaskLane.Api/Program.cs`, add `UseAuthentication`/`UseAuthorization`, and mark `MapHealthChecks`, `MapOpenApi` and `MapScalarApiReference` with `.AllowAnonymous()`.
  - Make the 500 case of T012 pass: in non-Development environments, the exception handler must not include exception details.
- [X] T015 [P] Write failing unit tests for the mapping in `tests/TaskLane.Api.Tests/ResultMappingsTests.cs`:
  - Validation → 400 `ValidationProblemDetails`, with field names in camelCase.
  - Conflict → 409, Unauthorized → 401 and NotFound → 404, each as `ProblemDetails` with `title` and `detail`.
  - Success → the supplied success result.
- [X] T016 Implement `ResultMappings` (extension methods on `ControllerBase` that map `Result`/`Result<T>` to `IActionResult`) in `src/TaskLane.Api/Common/ResultMappings.cs` until T015 passes.
- [X] T017 [P] Extend `tests/TaskLane.ArchitectureTests/FrameworkIndependenceTests.cs` so it asserts that Domain and Application do not reference `Microsoft.AspNetCore.Identity`, `Microsoft.IdentityModel` or `FluentValidation` (Domain only), and that Application does not reference `Microsoft.AspNetCore.Authentication`. Run it; it must pass.

**Checkpoint**: `dotnet test --solution TaskLane.slnx` is green, every endpoint requires authentication unless it opts out, and the `Users` table exists.

---

## Phase 3: User Story 1 - Create an account (Priority: P1) 🎯 MVP

**Goal**: A visitor registers with display name, email and password. The account is stored with a salted hash, duplicates are rejected with 409, and the SPA then shows sign-in with the email pre-filled.

**Independent Test**: With `POST /api/auth/register`, a new email returns 201 and the same email in a different case or with extra spaces returns 409. The stored `PasswordHash` ≠ the password. In the SPA, registering lands on `/sign-in` with the email pre-filled.

### Tests for User Story 1 (write first, must fail)

- [ ] T018 [P] [US1] Write failing validator tests in `tests/TaskLane.UnitTests/Identity/Register/RegisterUserValidatorTests.cs`, asserting each rule and message from data-model.md:
  - displayName "Not empty after trim; ≤ 100 chars after trim".
  - email "Not empty; valid email format; ≤ 254 chars after trim".
  - password "8–128 chars; not whitespace-only".
  - All failing fields are reported together.
- [ ] T019 [P] [US1] Write failing handler tests in `tests/TaskLane.UnitTests/Identity/Register/RegisterUserHandlerTests.cs`, with NSubstitute fakes for `IUserRepository`/`IPasswordHasher`, `FakeTimeProvider` and `FakeLogger<RegisterUserHandler>`:
  - Valid input → `TryAddAsync` receives a `User` whose `PasswordHash` is the hasher output, `CreatedAt` equals the fake clock and `Id` is a v7 Guid. Returns `RegisterUserResponse(Id, DisplayName, Email, CreatedAt)` and logs `UserRegistered` with the user id and without the email.
  - Invalid input → `Error.Validation` and the hasher is not called.
  - `TryAddAsync` returns false → `Error.Conflict("Email already registered", "An account with this email already exists.")`.
- [ ] T020 [P] [US1] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Identity/UserRepositoryTests.cs` and `PasswordHasherAdapterTests.cs`:
  - `TryAddAsync` returns true and persists a user.
  - It returns false for a duplicate normalized email, including two separate `AppDbContext` instances on the same connection inserting the same email: exactly one true.
  - The hash ≠ the password, and two hashes of the same password differ (SC-003).
  - `Verify` returns true for the right password and false for a wrong one.
- [ ] T021 [P] [US1] Write failing API tests in `tests/TaskLane.Api.Tests/Identity/RegisterEndpointTests.cs`:
  - 201 with `UserProfile` body and `Location: /api/users/me`.
  - 400 with `errors.displayName`/`errors.email`/`errors.password`.
  - 409 Problem Details for `" ANA@Example.com "` after `ana@example.com`.
  - The response never contains the password.
  - The endpoint is anonymous.

### Implementation for User Story 1

- [ ] T022 [P] [US1] Create the ports in `src/TaskLane.Application/Identity/Ports/`:
  - `IUserRepository.cs`, with `Task<bool> TryAddAsync(User, CancellationToken)`.
  - `IPasswordHasher.cs`, with `string Hash(string password)` and `bool Verify(string passwordHash, string password)`.
- [ ] T023 [US1] Implement the register slice in `src/TaskLane.Application/Identity/Register/` until T018 and T019 pass:
  - Files: `RegisterUserCommand.cs`, `RegisterUserValidator.cs`, `RegisterUserResponse.cs`, `RegisterUserHandler.cs`.
  - Logging: `IdentityLog.UserRegistered` in `src/TaskLane.Application/Identity/IdentityLog.cs`, using `[LoggerMessage]`.
  - Register the handler and validators in `src/TaskLane.Application/DependencyInjection.cs`.
- [ ] T024 [P] [US1] Implement `UserRepository.TryAddAsync`, which catches `DbUpdateException` with an inner `SqliteException` of extended code 2067 and returns false, in `src/TaskLane.Infrastructure/Identity/UserRepository.cs`. Implement `PasswordHasherAdapter` (wrapping `PasswordHasher<User>`, where `SuccessRehashNeeded` counts as success) in `src/TaskLane.Infrastructure/Identity/PasswordHasherAdapter.cs`. Register both as scoped/singleton in `src/TaskLane.Infrastructure/DependencyInjection.cs`. T020 must then pass.
- [ ] T025 [US1] Implement `POST /api/auth/register` in `src/TaskLane.Api/Identity/AuthController.cs` with `[AllowAnonymous]`, `[ApiController]` and `[Route("api/auth")]`, until T021 passes. It returns `CreatedResult` to `/api/users/me` with the profile body and uses `ResultMappings`. Add `ProducesResponseType` attributes for OpenAPI.
- [ ] T026 [P] [US1] Write failing Vitest tests:
  - In `frontend/src/features/auth/auth-api.test.ts`, `register()` posts JSON to `/api/auth/register` and surfaces 400/409 as `ApiError`.
  - In `frontend/src/features/auth/RegisterPage.test.tsx`:
    - Labelled fields for display name, email and password.
    - Client-side Zod errors use the same rules and messages as data-model.md, shown next to their fields with `aria-invalid`/`aria-describedby`.
    - The submit button is disabled while pending, with "Creating account…".
    - A 400 maps `errors` to fields, keeps display name and email, and clears the password (US1-3).
    - A 409 shows "This email is already registered." on the email field.
    - Keyboard only: Tab moves through the fields in order and Enter submits (FR-017).
    - A network error shows "We couldn't reach the service. Please try again." and the values are kept except the password.
    - Success navigates to `/sign-in` with the email pre-filled and "Account created. You can now sign in.".
- [ ] T027 [US1] Implement `frontend/src/features/auth/auth-api.ts` (`register`) and `frontend/src/features/auth/RegisterPage.tsx` (React Hook Form + Zod schema in `frontend/src/features/auth/schemas.ts`, `useMutation`). Add the `/register` route in `frontend/src/app/routes.tsx` and a minimal `/sign-in` placeholder that reads the pre-filled email from navigation state. T026 must then pass.

**Checkpoint**: US1 works on its own through the API and the SPA, and the full test suite is green.

---

## Phase 4: User Story 2 - Sign in and reach protected content (Priority: P1)

**Goal**: Sign in with email and password to get a 60-minute token, then view the profile. Failed sign-ins are indistinguishable from each other. Anonymous access to the profile goes to sign-in and comes back safely.

**Independent Test**: `POST /api/auth/sign-in` returns 200 with a token for correct credentials, and the same 401 body for a wrong password and for an unknown email. `GET /api/users/me` returns 200 with the token, and 401 for missing, expired, malformed, tampered, foreign-key and orphaned tokens. In the SPA, an anonymous visit to `/profile` goes to sign-in and returns to the profile.

### Tests for User Story 2 (write first, must fail)

- [ ] T028 [P] [US2] Write failing unit tests in `tests/TaskLane.UnitTests/Identity/SignIn/SignInValidatorTests.cs` (email and password "Not empty") and `SignInHandlerTests.cs`:
  - Correct credentials → `SignInResponse(AccessToken, "Bearer", ExpiresAt)` from the `IAccessTokenIssuer` fake, logging `SignInSucceeded(userId)`.
  - Wrong password and unknown email → the same `Error.Unauthorized("Invalid credentials", "Invalid email or password.")`, logging `SignInFailed` with no email.
  - Unknown email still calls `IPasswordHasher.Verify` once, against the dummy hash (research R4).
  - Lookup uses `User.NormalizeEmail`.
- [ ] T029 [P] [US2] Write failing unit tests in `tests/TaskLane.UnitTests/Identity/GetProfile/GetProfileHandlerTests.cs`:
  - An existing id → `ProfileResponse(Id, DisplayName, Email, CreatedAt)`.
  - A missing id → `Error.Unauthorized` (Clarification Q3).
- [ ] T030 [P] [US2] Write failing Infrastructure tests:
  - `tests/TaskLane.Infrastructure.Tests/Identity/UserRepositoryTests.cs`: `GetByNormalizedEmailAsync` and `GetByIdAsync` return the user or null.
  - `tests/TaskLane.Infrastructure.Tests/Identity/JwtAccessTokenIssuerTests.cs`: the token validates with the configured key, issuer and audience; claims are exactly `sub` = user id plus `jti`, `iat`, `nbf`, `exp`, `iss`, `aud`, with no email; `exp` and `ExpiresAt` = `FakeTimeProvider` now + 60 min; the algorithm is HS256.
- [ ] T031 [P] [US2] Write failing API tests in `tests/TaskLane.Api.Tests/Identity/SignInEndpointTests.cs`:
  - 200 with `accessToken`, `tokenType`, `expiresAt`.
  - 400 for empty fields.
  - The wrong-password and unknown-email 401 responses have byte-identical bodies (ignoring `traceId`) and equal status and headers (SC-004).
- [ ] T032 [P] [US2] Write failing API tests in `tests/TaskLane.Api.Tests/Identity/ProfileEndpointTests.cs`:
  - 200 profile with a valid token, with no `passwordHash`/`password` fields.
  - 401 Problem Details for each of: missing header; expired token (advance `FakeTimeProvider` by 60 min); malformed string; tampered payload; token signed with another key; wrong issuer; wrong audience; a token for a deleted user (SC-002).
- [ ] T033 [P] [US2] Write a failing API test in `tests/TaskLane.Api.Tests/Identity/AuthLoggingTests.cs`. It runs register, sign-in success, sign-in failure and profile. It asserts that `FakeLogCollector` has the `UserRegistered`, `SignInSucceeded` and `SignInFailed` events, and that no record's message or state contains the test password, email (any case) or issued token (SC-007).

### Implementation for User Story 2

- [ ] T034 [P] [US2] Add `GetByNormalizedEmailAsync(string, CancellationToken)` and `GetByIdAsync(Guid, CancellationToken)` to `src/TaskLane.Application/Identity/Ports/IUserRepository.cs` and implement them with `AsNoTracking` in `src/TaskLane.Infrastructure/Identity/UserRepository.cs`. Create `src/TaskLane.Application/Identity/Ports/IAccessTokenIssuer.cs`, which returns `AccessToken(string Value, DateTimeOffset ExpiresAt)`.
- [ ] T035 [US2] Implement the sign-in slice in `src/TaskLane.Application/Identity/SignIn/` until T028 passes:
  - Files: `SignInCommand.cs`, `SignInValidator.cs`, `SignInResponse.cs`, `SignInHandler.cs`.
  - The handler keeps a static dummy hash generated once with `IPasswordHasher` for timing equalization.
  - Add `SignInSucceeded`/`SignInFailed` to `src/TaskLane.Application/Identity/IdentityLog.cs`.
  - Register the handler in DI.
- [ ] T036 [US2] Implement the profile slice in `src/TaskLane.Application/Identity/GetProfile/` (`GetProfileQuery.cs`, `ProfileResponse.cs`, `GetProfileHandler.cs`) until T029 passes. Register it in DI.
- [ ] T037 [P] [US2] Implement `JwtAccessTokenIssuer`, using `JsonWebTokenHandler`, HS256, `TimeProvider` and `JwtOptions`, in `src/TaskLane.Infrastructure/Identity/JwtAccessTokenIssuer.cs`. Register it in `src/TaskLane.Infrastructure/DependencyInjection.cs`. T030 must then pass.
- [ ] T038 [US2] Add `POST /api/auth/sign-in` to `src/TaskLane.Api/Identity/AuthController.cs` (anonymous). Create `src/TaskLane.Api/Identity/UsersController.cs` with `GET /api/users/me`, which reads the `sub` claim and maps an absent or unparsable claim to 401. T031, T032 and T033 must then pass.
- [ ] T039 [P] [US2] Write failing Vitest tests:
  - `frontend/src/shared/api/http-client.test.ts`: `request` adds `Authorization: Bearer` when a token is given.
  - `frontend/src/features/auth/return-path.test.ts`: `/profile` ✅; `https://evil.example`, `//evil.example`, `/\evil.example` and `/does-not-exist` → `/profile`; the query string is preserved.
  - `frontend/src/features/auth/session/SessionProvider.test.tsx`: `signIn` stores `{accessToken, expiresAt}` under `tasklane.session`; invalid JSON is discarded.
  - `frontend/src/features/auth/SignInPage.test.tsx`: labelled fields; disabled "Signing in…" while pending; 401 → "Invalid email or password." with the password cleared; a network error → "We couldn't reach the service. Please try again." with the email kept and the password cleared; keyboard-only submit with Enter; success → safe `returnTo` or `/profile`.
  - `frontend/src/features/auth/RequireAuth.test.tsx`: anonymous → `/sign-in?returnTo=%2Fprofile`.
  - `frontend/src/features/profile/ProfilePage.test.tsx`: shows display name, email and member-since date in local format.
- [ ] T040 [US2] Implement the SPA until T039 passes:
  - Add an optional token to `frontend/src/shared/api/http-client.ts`.
  - Session context: `frontend/src/features/auth/session/session-storage.ts`, `SessionProvider.tsx` and `useSession.ts`.
  - Return-path guard: `frontend/src/features/auth/return-path.ts`, using `matchRoutes` against `routes`.
  - `signIn` in `frontend/src/features/auth/auth-api.ts`.
  - Replace the `/sign-in` placeholder with `frontend/src/features/auth/SignInPage.tsx`.
  - Add `frontend/src/features/auth/RequireAuth.tsx`, `frontend/src/features/profile/profile-api.ts` and `frontend/src/features/profile/ProfilePage.tsx`.
  - Add `/profile` behind `RequireAuth` in `frontend/src/app/routes.tsx` and wrap the app with `SessionProvider` in `frontend/src/app/App.tsx`.
  - Placement: `SessionProvider` (state and storage only, no navigation) wraps `QueryClientProvider` and `RouterProvider` in `App.tsx`. Anything that navigates (guards, and the 401 handling added in T042) runs inside the router, in `RequireAuth` and `frontend/src/shared/layout/AppLayout.tsx`, and calls `useSession()`. `App.tsx` creates the `QueryClient` with `QueryCache`/`MutationCache` `onError` callbacks that publish 401s to the session context, which marks the session as rejected so that `AppLayout` redirects.

**Checkpoint**: US1 and US2 work together: register, sign in, view the profile, and are protected against anonymous access.

---

## Phase 5: User Story 3 - Session lifecycle (Priority: P2)

**Goal**: The session survives reloads in the same tab, sign-out ends it, an expired session sends the person to sign-in with the expiry message, and signed-in people are kept away from the sign-in and registration screens.

**Independent Test**: Sign in and reload; you are still on the profile. Sign out; `/profile` requires signing in. Set an expired `expiresAt`; the expiry message appears. Visiting `/sign-in` while signed in redirects to `/profile`.

### Tests for User Story 3 (write first, must fail)

- [ ] T041 [P] [US3] Write failing Vitest tests:
  - `frontend/src/features/auth/session/SessionProvider.test.tsx`:
    - A session is restored from `sessionStorage` on mount (reload).
    - `signOut` clears storage and the query cache.
    - Any `ApiError` with status 401 from an authenticated query clears the session and navigates to `/sign-in?returnTo=…`.
    - The state carries `reason: 'expired'` only when `expiresAt` ≤ now (fake timers).
  - `frontend/src/features/auth/RequireAuth.test.tsx`: an expired stored session → redirect with the expiry message.
  - `frontend/src/features/auth/SignInPage.test.tsx`: the expired reason shows "Your session has expired. Please sign in again." with `role="alert"`.
  - `frontend/src/features/auth/RedirectIfAuthenticated.test.tsx`: a signed-in user visiting `/sign-in` or `/register` → `/profile`.
  - `frontend/src/features/profile/ProfilePage.test.tsx`: "Sign out" → `/sign-in`, and `/profile` then redirects.

### Implementation for User Story 3

- [ ] T042 [US3] Implement the session lifecycle until T041 passes:
  - 401 handling as placed in T040: `onError` callbacks in `frontend/src/app/App.tsx` notify `frontend/src/features/auth/session/SessionProvider.tsx`, and the redirect is performed in `frontend/src/shared/layout/AppLayout.tsx`.
  - Expiry check in `frontend/src/features/auth/RequireAuth.tsx`.
  - `frontend/src/features/auth/RedirectIfAuthenticated.tsx` wrapping `/sign-in` and `/register` in `frontend/src/app/routes.tsx`.
  - A sign-out button in `frontend/src/features/profile/ProfilePage.tsx`.
  - Session-aware navigation links (sign in/register vs profile) in `frontend/src/shared/layout/AppLayout.tsx`.

**Checkpoint**: Every acceptance scenario of US3 passes in Vitest.

---

## Phase 6: User Story 4 - Demo account available out of the box (Priority: P3)

**Goal**: A fresh Development run has a working demo account, documented in the README, provisioned idempotently.

**Independent Test**: Delete `tasklane.db` and run the API; signing in as `demo@tasklane.dev` / `TaskLane-Demo-2026` returns 200. Restart; still exactly one demo user with the same hash.

### Tests for User Story 4 (write first, must fail)

- [ ] T043 [P] [US4] Write failing Infrastructure tests in `tests/TaskLane.Infrastructure.Tests/Identity/DemoAccountSeederTests.cs`:
  - Seeding an empty migrated database creates one user with the configured email and display name, whose password verifies.
  - Seeding twice leaves one user with an unchanged `PasswordHash`.
  - An existing account with the demo email but another password is not modified.
  - Missing `DemoAccount` configuration → no-op.
  - Also write a failing API test in `tests/TaskLane.Api.Tests/Identity/DemoAccountTests.cs`: in the non-Development test host, signing in with the demo credentials returns 401, so the demo account is never provisioned outside Development (spec Assumptions, FR-016).

### Implementation for User Story 4

- [ ] T044 [US4] Implement `DemoAccountSeeder` in `src/TaskLane.Infrastructure/Identity/DemoAccountSeeder.cs` until T043 passes:
  - Options `DemoAccount:Email`, `DemoAccount:Password`, `DemoAccount:DisplayName`.
  - It uses `User.Register`, `IPasswordHasher`, `TimeProvider` and `IUserRepository`.
  - Call it from `InitializeDatabaseAsync` in `src/TaskLane.Infrastructure/Persistence/DatabaseInitializer.cs`, which runs only in Development (`src/TaskLane.Api/Program.cs`).
  - Add the `DemoAccount` section (`demo@tasklane.dev`, `TaskLane-Demo-2026`, `Demo User`) to `src/TaskLane.Api/appsettings.Development.json`.
- [ ] T045 [US4] Document the demo credentials and the "Development only" note in `README.md`, under a "Demo account" section.

**Checkpoint**: All four user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T046 [P] Amend `docs/adr/0006-jwt-bearer-authentication.md`: change the claims to `sub` and `jti`; there is no sign-out endpoint and sign-out is client-side only (accepted risk, spec Clarification Q1); clock skew is 0; failed sign-in timing is equalized. Replace "login" with "sign in" in `docs/adr/0006-jwt-bearer-authentication.md` and `docs/adr/0010-http-api-conventions.md` where it refers to the user action.
- [ ] T047 [P] Update `docs/architecture.md` with the Identity module slices, the request flow (controller → handler → ports) and the auth pipeline. Update the "Where things go" examples in `CLAUDE.md` only if they changed.
- [ ] T048 [P] Add sample requests for register, sign-in and me (with a `@token` variable) to `src/TaskLane.Api/TaskLane.Api.http`.
- [ ] T049 Run the automated gates from `specs/001-user-authentication/quickstart.md` §1:
  - `dotnet build` (0 warnings).
  - `dotnet format --verify-no-changes`.
  - `./scripts/test-coverage.ps1`: ≥ 90 % Domain+Application, ≥ 80 % overall.
  - `npm run check`.

  Fix any failure.
- [ ] T050 Run the manual API and SPA checks from `specs/001-user-authentication/quickstart.md` §2–§3, including 360 px width, visible focus, keyboard-only use and a clean console. These checks (FR-017, SC-001, SC-006) are deliberately manual, because jsdom cannot measure layout or console output from a real browser. The user performs the browser part, and the observed results are recorded in the PR description.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup; blocks all user stories.
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**:
  - **Backend:** depends on Foundational. It reuses the `IUserRepository`/`IPasswordHasher` ports from T022/T024. If US2 is started before US1, do T022 and T024 first.
  - **SPA:** reuses `auth-api.ts` and the `/sign-in` route from T027.
- **US3 (Phase 5)**: depends on the US2 SPA session (T040).
- **US4 (Phase 6)**: depends on Foundational plus T022/T024 (repository and hasher). It is independent of US2 and US3.
- **Polish (Phase 7)**: after the desired stories are complete.

### Within Each Story

- Tests (Red) → implementation (Green) → refactor, committed separately.
- Ports → Application slice → Infrastructure adapters → Api controller → SPA.

### Parallel Opportunities

- **Setup:** T003 and T004.
- **Foundational:** T005, T007, T009, T012, T013, T015 and T017 touch different files and can be written in parallel.
- **US1:** T018–T021 (tests) in parallel. Then T022 and T024 in parallel with T023. T026 can start once the contract is fixed.
- **US2:** T028–T033 in parallel. T034 and T037 in parallel. Frontend T039/T040 in parallel with backend T035–T038.
- **US4:** can proceed in parallel with US2/US3 once T024 is done.

## Parallel Example: User Story 2

```text
# Red, all in parallel:
T028 SignIn validator + handler unit tests
T029 GetProfile handler unit tests
T030 Repository lookups + JWT issuer tests
T031 Sign-in endpoint tests
T032 Profile endpoint 401 matrix
T033 Log redaction test

# Green, in parallel where marked:
T034 Repository lookups + IAccessTokenIssuer port
T037 JwtAccessTokenIssuer
```

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) and Phase 2 (Foundational).
2. Phase 3 (US1). **Stop and validate**: registration works end to end and duplicates are rejected.

### Incremental Delivery

1. US1 → registration.
2. US2 → sign-in and profile. This completes the brief's "authorized and non-authorized endpoints" requirement.
3. US3 → session lifecycle polish.
4. US4 → demo account for the review session.
5. Polish → docs and gates, then a PR to `main`.

## Notes

- `[P]` = different files and no dependency on an unfinished task.
- Every Green task names the test task it must make pass. Never weaken a test to get to green.
- Commit after each Red, Green and Refactor step (`test:`, `feat:`, `refactor:`), without co-author trailers.
- Migrations, configuration and docs are exempt from test-first (constitution II).
