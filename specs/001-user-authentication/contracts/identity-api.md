# Contract: Identity HTTP API

Conventions from ADR 0010:

- JSON bodies use camelCase.
- Errors are RFC 9457 Problem Details (`application/problem+json`) and never include stack
  traces or internal details (FR-015).
- Every endpoint requires `Authorization: Bearer <accessToken>` unless marked **Anonymous**.

## `POST /api/auth/register` — Anonymous

Request:

```json
{ "displayName": "Ana", "email": "ana@example.com", "password": "correct horse battery" }
```

| Status | When | Body |
|---|---|---|
| `201 Created` | Account created. `Location: /api/users/me` | `UserProfile` |
| `400 Bad Request` | One or more fields invalid | Validation Problem Details: `errors` keyed by `displayName`, `email`, `password` (array of messages each) |
| `409 Conflict` | Normalized email already registered | Problem Details, `title: "Email already registered"`, `detail: "An account with this email already exists."` The SPA shows its own field-level wording, defined in [spa-routes.md](spa-routes.md) |

No token is returned: registration never starts a session (FR-005).

## `POST /api/auth/sign-in` — Anonymous

Request:

```json
{ "email": "ana@example.com", "password": "correct horse battery" }
```

| Status | When | Body |
|---|---|---|
| `200 OK` | Credentials match | `{ "accessToken": "<jwt>", "tokenType": "Bearer", "expiresAt": "2026-11-18T10:00:00+00:00" }` |
| `400 Bad Request` | Email or password empty | Validation Problem Details |
| `401 Unauthorized` | Unknown email **or** wrong password, with identical body and headers in both cases | Problem Details, `title: "Invalid credentials"`, `detail: "Invalid email or password."` |

## `GET /api/users/me` — Authenticated

| Status | When | Body |
|---|---|---|
| `200 OK` | Valid token for an existing account | `UserProfile` |
| `401 Unauthorized` | Token missing, expired, malformed, tampered, signed with another key, wrong issuer/audience, or its account no longer exists | Problem Details (status 401), `WWW-Authenticate: Bearer` when the token itself is rejected |

## `UserProfile`

```json
{
  "id": "0193a5c8-6f1e-7b2a-9c4d-2e8f1a3b5c7d",
  "displayName": "Ana",
  "email": "ana@example.com",
  "createdAt": "2026-11-18T09:00:00+00:00"
}
```

`email` is returned as entered (trimmed). There are no credential fields (FR-011).

## Access token

| Property | Value |
|---|---|
| Format | JWT (JWS compact) |
| Algorithm | HS256 |
| Lifetime | 60 minutes, no refresh |
| Claims | `iss`, `aud`, `sub` (user id), `jti`, `iat`, `nbf`, `exp` |
| Validated | Signature, algorithm, issuer, audience, lifetime (clock skew 0) |

## Unchanged public endpoints

- `GET /health` — Anonymous (FR-010 "service status check").
- `GET /openapi/v1.json` and `/scalar/v1` — Anonymous, Development only.
- Any other new endpoint is authenticated by default through the fallback authorization policy.
