# 0005. Password hashing with `PasswordHasher<T>`

- Status: Accepted (implemented by feature `001-user-authentication`)
- Date: 2026-11-17

## Context

User registration requires storing credentials securely. Custom cryptography is forbidden by the
constitution (principle VI).

## Decision

Use `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` from the
`Microsoft.Extensions.Identity.Core` package, wrapped behind an Application port
(`IPasswordHasher`) and implemented in Infrastructure. It provides PBKDF2 with HMAC-SHA512, a
random per-password salt, a versioned hash format, and `SuccessRehashNeeded` so the work factor
can be raised later without breaking existing hashes.

## Alternatives considered

- **Full ASP.NET Core Identity** (`UserManager`, `IdentityDbContext`): battle-tested, but imposes
  its schema (several tables), its abstractions, and hides the registration and sign-in logic that
  this project needs to own and test.
- **BCrypt.Net-Next / Argon2 libraries**: strong algorithms, but add a third-party dependency
  without a meaningful benefit over the framework-maintained hasher.

## Consequences

- No third-party crypto dependency; algorithm upgrades come with .NET servicing.
- The port keeps Application independent of the hashing implementation and easy to fake in tests.
