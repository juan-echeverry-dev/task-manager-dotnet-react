# Quickstart: validate feature 001

How to prove the feature works end to end. The contracts are in
[contracts/identity-api.md](contracts/identity-api.md) and
[contracts/spa-routes.md](contracts/spa-routes.md); the entity is in
[data-model.md](data-model.md).

## Prerequisites

- .NET SDK 10.0.400+, Node 24+, a fresh clone on branch `001-user-authentication`.
- `dotnet tool restore` and `cd frontend; npm ci`.

## 1. Automated gates

```powershell
dotnet build TaskLane.slnx                      # 0 warnings
dotnet format TaskLane.slnx --verify-no-changes
./scripts/test-coverage.ps1                     # all tests + coverage ≥ 90 % Domain+Application, ≥ 80 % overall
cd frontend; npm run check                      # typecheck, oxlint (0 warnings), Vitest, build
```

Expected test coverage by layer:

| Layer | Must prove |
|---|---|
| Unit | `User` invariants and normalization; validators (every rule in data-model); handlers: success, 400, 409, uniform 401, orphaned profile → unauthorized; auth events logged (FakeLogger) |
| Infrastructure | Migration creates `Users` with a unique `NormalizedEmail`; `TryAddAsync` returns `false` on a duplicate, including two contexts inserting concurrently; hash ≠ password and two equal passwords hash differently; token claims and expiry; the demo seeder is idempotent and leaves the password unchanged |
| Api | Each status code in the contract; protected endpoint refuses missing/expired/malformed/tampered/foreign-key/orphaned tokens; identical 401 bodies for unknown email vs wrong password; no log record contains the password, email or token |
| Architecture | Existing dependency rules still pass with the new packages |
| Frontend | Forms (validation, pending state, server errors); session storage; guards; safe `returnTo`; 401 → expiry message |

## 2. Manual API check (Development)

```powershell
dotnet run --project src/TaskLane.Api           # http://localhost:5208, Scalar at /scalar/v1
```

1. `POST /api/auth/register` with a new email → `201`. Repeat it with `" ANA@Example.com "` → `409`.
2. `POST /api/auth/sign-in` with a wrong password and then with an unknown email → two identical
   `401` bodies.
3. `POST /api/auth/sign-in` with the correct password → `200` with `accessToken`.
4. `GET /api/users/me` without a token → `401`; with `Authorization: Bearer <token>` → `200`
   profile.
5. Sign in as `demo@tasklane.dev` / `TaskLane-Demo-2026` → `200`. Restart the API and sign in
   again → still `200`.
6. Inspect `tasklane.db` (`Users` table): `PasswordHash` never equals the password.

## 3. Manual SPA check

```powershell
cd frontend; npm run dev                        # http://localhost:5173 (proxies /api to 5208)
```

1. Open `/profile` while signed out → redirected to `/sign-in?returnTo=%2Fprofile`.
2. Register → land on sign-in with the email pre-filled and a success message.
3. Sign in → back on `/profile` showing name, email and member-since date. Reload → still signed
   in.
4. Open `/sign-in` → redirected to `/profile`. Sign out → on `/sign-in`; `/profile` requires
   signing in again.
5. Open `/sign-in?returnTo=https://evil.example` and sign in → land on `/profile`.
6. Expiry: in DevTools, set `expiresAt` in `sessionStorage` to a past instant and reload
   `/profile` → sign-in screen with "Your session has expired. Please sign in again."
7. At 360 px width there is no horizontal scroll, the flows can be completed with the keyboard
   only, and the console stays clean.
