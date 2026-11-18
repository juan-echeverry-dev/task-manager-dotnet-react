# Data Model: User Registration and Authentication

## Entity: `User` (Domain, `TaskLane.Domain.Identity`)

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Version 7 (time-ordered), assigned on creation; never changes |
| `Email` | `string` | Email as entered, surrounding spaces trimmed; ≤ 254 chars |
| `NormalizedEmail` | `string` | `Email.ToLowerInvariant()`; unique across all users |
| `DisplayName` | `string` | Trimmed; 1–100 chars |
| `PasswordHash` | `string` | Output of the password hasher; never the password itself |
| `CreatedAt` | `DateTimeOffset` | UTC instant from `TimeProvider` at registration |

**Creation**: `User.Register(id, email, displayName, passwordHash, createdAt)` is the only way to
build a `User`. It trims and normalizes, and it throws `ArgumentException` when an invariant is
broken (empty or over-long values, non-UTC `createdAt`). Invariant violations are programming
errors: user input is validated before this point by the Application validator, which reports
field errors as values.

**Lifecycle**: Created → (no further transitions in this feature). Nothing in this feature
updates or deletes a user.

**Helper**: `User.NormalizeEmail(string)` (static) normalizes the email used to look up a
sign-in, so registration and sign-in share exactly one rule.

## Table: `Users` (Infrastructure, EF Core configuration + migration `AddUsers`)

| Column | SQLite type | Constraints |
|---|---|---|
| `Id` | TEXT | Primary key |
| `Email` | TEXT | NOT NULL, max 254 |
| `NormalizedEmail` | TEXT | NOT NULL, max 254, **unique index** `IX_Users_NormalizedEmail` |
| `DisplayName` | TEXT | NOT NULL, max 100 |
| `PasswordHash` | TEXT | NOT NULL |
| `CreatedAt` | TEXT | NOT NULL (ISO 8601 UTC; no ordering queries in this feature) |

## Input validation (Application, FluentValidation)

| Use case | Field | Rule | Message |
|---|---|---|---|
| Register | `displayName` | Not empty after trim; ≤ 100 chars after trim | "Display name is required." / "Display name must be at most 100 characters." |
| Register | `email` | Not empty; valid email format; ≤ 254 chars after trim | "Email is required." / "Enter a valid email address." / "Email must be at most 254 characters." |
| Register | `password` | 8–128 chars; not whitespace-only | "Password must be between 8 and 128 characters." / "Password cannot be only spaces." |
| Sign in | `email`, `password` | Not empty | "Email is required." / "Password is required." |

Sign-in deliberately does not apply the registration password rules: a password that would fail
them simply does not match, and the person gets the uniform failure message (FR-007).

## Non-persisted concepts

- **Access token** (session credential): signed token with claims `sub` (user id) and `jti`;
  valid 60 minutes from issue; never stored by the server (spec: Session entity).
- **Client session** (SPA): `{ accessToken, expiresAt }` in `sessionStorage`, key
  `tasklane.session`.

## Demo account (Development only)

| Field | Value |
|---|---|
| Email | `demo@tasklane.dev` |
| Password | `TaskLane-Demo-2026` |
| Display name | `Demo User` |

Configured under `DemoAccount` in `appsettings.Development.json` and documented in the README.
The account is created only when no user has that normalized email; an existing account is never
changed (FR-016).
