# Research: User Registration and Authentication

Phase 0 output for [plan.md](plan.md). The Technical Context had no `NEEDS CLARIFICATION`
items: the stack is fixed by ADRs 0002–0010. The entries below settle the remaining design
questions, one decision each.

## R1. Email normalization and uniqueness

- **Decision**: The Domain owns normalization: `Trim()` for the stored email ("as entered") and
  `Trim().ToLowerInvariant()` for `NormalizedEmail`. A **unique index on `NormalizedEmail`**
  enforces uniqueness in the database.
- **Rationale**: FR-002 and FR-014 require both forms. The unique index is the only reliable
  guard against concurrent registrations (spec edge case), and constitution VI requires it.
- **Alternatives**: `COLLATE NOCASE` on a single column only covers ASCII case folding and hides
  the rule in the database. `ToUpperInvariant`, the ASP.NET Identity convention, is equivalent;
  lower case is easier to read in the database.

## R2. Registration flow under concurrency

- **Decision**: The handler validates, hashes, builds the `User` and calls
  `IUserRepository.TryAddAsync(user)`. That method saves and returns `false` when SQLite reports a
  unique-constraint violation (`SqliteException`, extended code 2067), which the handler turns
  into `Error.Conflict`. There is no separate "exists?" query beforehand.
- **Rationale**: One code path covers both the normal duplicate and the concurrent one, so there
  is no check-then-act race. The repository saving directly is acceptable because `DbContext` is
  already the unit of work (ADR 0004).
- **Alternatives**: "Exists" check plus insert races, and it still needs the constraint handler.
  A serializable transaction is overkill.

## R3. Password hashing

- **Decision**: `IPasswordHasher` port with `Hash(password)` and `Verify(hash, password)`,
  implemented with `PasswordHasher<User>` (PBKDF2-HMAC-SHA512, 100k iterations, per-hash salt).
  `SuccessRehashNeeded` counts as a successful verification; rehashing is out of scope.
- **Rationale**: ADR 0005. The port keeps Application testable with a fake.

## R4. Uniform failed sign-in (FR-007, SC-004)

- **Decision**: Unknown email and wrong password return the same `Error.Unauthorized` (401,
  "Invalid email or password."). When the email is unknown, the handler still verifies the
  password against a fixed dummy hash, so both paths take comparable time.
- **Rationale**: Identical messages alone still leak through response time. The dummy check
  costs one hash.
- **Alternatives**: Ignore timing (simpler, but leaks); lockout/rate limiting is out of scope
  (spec assumptions).

## R5. Access token issuance and validation

- **Decision**:
  - **Issuing**: Infrastructure `JwtAccessTokenIssuer` uses `JsonWebTokenHandler`, HS256 and
    `TimeProvider`.
    - Claims are `sub` (user id) and `jti` only.
    - Lifetime is 60 min and the response includes `expiresAt`.
  - **Options**: `JwtOptions` holds `Issuer`, `Audience`, `SigningKey` (≥ 32 bytes) and
    `LifetimeMinutes` (60). They are bound from `Authentication:Jwt` and validated with
    `ValidateOnStart`.
  - **Key source**: a dev-only key is in `appsettings.Development.json`; tests supply their own.
  - **Validation (Api)**: `AddJwtBearer` validates issuer, audience, lifetime, signature and
    algorithm.
    - `ClockSkew = 0`, so the 60 minutes are exact.
    - `MapInboundClaims = false`, so `sub` stays `sub`.
    - It uses the same `TimeProvider`.
- **Rationale**: ADR 0006. The `email` claim is dropped: nothing reads it, and the profile
  endpoint supplies the email. ADR 0006 gets a one-line amendment.
- **Alternatives**: `JwtSecurityTokenHandler` is legacy and slower. Asymmetric keys (RS256) only
  pay off when another service validates the tokens (single host, ADR 0003).

## R6. Sign-out and session expiry (FR-008, FR-009, US3)

- **Decision**:
  - **No sign-out endpoint**: sign-out clears `sessionStorage` and the query cache (Clarification
    Q1).
  - **Stored session**: `{ accessToken, expiresAt }`.
  - **Expiry detection**: a session is expired when `now ≥ expiresAt`.
    - **On a protected route**: if the stored session is expired (also right after a reload), the
      guard clears it and redirects to sign-in with the expiry message.
    - **On any 401 from an authenticated request**: the session is cleared and the person is sent
      to `/sign-in`. The expiry message is shown only if `expiresAt` has passed; a
      tampered/foreign token shows no message.
- **Rationale**: Matches the stateless design and the spec wording ("next action").
- **Alternatives**: A proactive timer that signs out at expiry gives better UX, but it is not
  required and is harder to test deterministically.

## R7. Orphaned sessions (Clarification Q3)

- **Decision**: `GetProfile` returns `Error.Unauthorized` when the `sub` user no longer exists,
  and the Api maps that to 401. The SPA treats it like any 401 (R6).
- **Rationale**: Same outcome as an invalid session, with no extra flow.

## R8. Return to the original screen (FR-012, Clarification Q4)

- **Decision**:
  - **Passing the destination**: `RequireAuth` redirects to `/sign-in?returnTo=<path+search>`.
  - **Validation**: after sign-in, `returnTo` is used only if it starts with a single `/` (not
    `//` or `/\`), parses as a same-origin URL, and matches a known route other than the
    catch-all `*` (`matchRoutes`).
  - **Fallback**: in every other case the person goes to `/profile`.
- **Rationale**: Blocks open redirects and unknown paths. It is a pure function, so it is easy to
  unit test.

## R9. Authentication-event logging (FR-019, SC-007)

- **Decision**:
  - **Events**: source-generated `LoggerMessage` methods in `Application/Identity/IdentityLog.cs`
    log `UserRegistered(userId)`, `SignInSucceeded(userId)` and `SignInFailed()`.
  - **Dependencies**: Application references only `Microsoft.Extensions.Logging.Abstractions`.
  - **Unit tests** use `FakeLogger` to assert the events and their fields.
  - **API tests** collect every log record with `FakeLogCollector` and assert that none contains
    the test password, email or issued token.
- **Rationale**: Testable, no new storage (Clarification Q2), and satisfies CA1848. The logging
  abstractions are not EF Core or ASP.NET Core, so principle III holds.

## R10. New dependencies

| Package | Version | Project | Reason | License |
|---|---|---|---|---|
| FluentValidation | 12.2.0 | Application | Input validation (ADR 0007) | Apache-2.0 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.15 | Application | Auth event logging (R9) | MIT |
| Microsoft.Extensions.Identity.Core | 10.0.15 | Infrastructure | `PasswordHasher<T>` (ADR 0005) | MIT |
| Microsoft.IdentityModel.JsonWebTokens | 8.x (aligned with JwtBearer's dependency) | Infrastructure | Token issuance (R5) | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.15 | Api | Token validation (R5) | MIT |
| NSubstitute | 6.2.0 | UnitTests | Port fakes (ADR 0008) | BSD-3-Clause |
| Microsoft.Extensions.TimeProvider.Testing | 10.11.0 | UnitTests, Infrastructure.Tests, Api.Tests | `FakeTimeProvider` | MIT |
| Microsoft.Extensions.Diagnostics.Testing | 10.11.0 | UnitTests, Api.Tests | `FakeLogger`, `FakeLogCollector` | MIT |
| react-hook-form | 7.x | frontend | Forms (ADR 0009) | MIT |
| zod | 4.x | frontend | Client-side validation schemas (ADR 0009) | MIT |
| @hookform/resolvers | 7.x | frontend | Zod ↔ React Hook Form bridge | MIT |

Pinning: Microsoft runtime packages stay on the 10.0.x servicing band (11.0 targets .NET 11). The
`Microsoft.Extensions.*.Testing` packages use their own 10.x line. Versions were checked against
nuget.org and npm on 2026-11-18. Exact frontend versions are fixed by `package-lock.json` on
install.

## R11. Test database for API tests

- **Decision**: `ApiFactory` replaces the `AppDbContext` options with one shared open in-memory
  `SqliteConnection` per factory and applies migrations when the host starts. Each test uses
  unique emails, so tests stay independent.
- **Rationale**: The current factory uses `Data Source=:memory:` without migrations, which is fine
  for `/health` but has no schema. Using real migrations keeps API tests faithful to the
  database.

## R12. Requirement traceability

| Requirement | Design element | Verified by |
|---|---|---|
| FR-001, FR-003, FR-005 | `Register` slice + validator; SPA `RegisterPage` → `/sign-in` with email | Unit (validator, handler), API (201/400), Vitest |
| FR-002, FR-004 | `User` normalization, unique index, `TryAddAsync` → 409 | Unit, Infrastructure (unique index, concurrent insert), API (409) |
| FR-006, FR-007 | `SignIn` slice, R4, R5 | Unit, API (200 / identical 401s) |
| FR-008, FR-009 | SPA session in `sessionStorage`, sign-out clears it (R6) | Vitest |
| FR-010 | Fallback policy + explicit `[AllowAnonymous]` | API (every protected endpoint × missing/expired/malformed/tampered/orphaned token) |
| FR-011 | `GetProfile` slice → `GET /api/users/me` | Unit, API |
| FR-012 | 401 → SPA redirect with safe `returnTo` (R6, R8) | API, Vitest |
| FR-013, FR-014 | `PasswordHasher` adapter; `Users` table | Infrastructure (hash ≠ password, same password → different hashes) |
| FR-015 | Problem Details everywhere, exception handler | API |
| FR-016 | `DemoAccountSeeder` (Development) | Infrastructure (idempotent, password unchanged) |
| FR-017, FR-018 | Accessible forms, disabled submit while pending | Vitest + manual check (quickstart) |
| FR-019 | `IdentityLog` (R9) | Unit (FakeLogger), API (FakeLogCollector redaction) |
