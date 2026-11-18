# 0006. JWT bearer authentication

- Status: Accepted (implemented by feature `001-user-authentication`)
- Date: 2026-11-17

## Context

The SPA must sign users in and call protected endpoints. The requirements ask for authorized and
non-authorized endpoints. The API should be easy to exercise from Scalar or any HTTP client.

## Decision

- **Access tokens are JWTs** issued by the Identity module (`JsonWebTokenHandler`, HS256) and
  validated with `Microsoft.AspNetCore.Authentication.JwtBearer`: issuer, audience, lifetime,
  and signature are all validated, with zero clock skew so the 60-minute lifetime is exact.
- Tokens are sent in the `Authorization: Bearer` header. Lifetime: **60 minutes**, no refresh
  tokens. Claims are minimal (`sub`, `jti`); no email or other personal data.
- The signing key comes from configuration (user-secrets or environment variables). A clearly
  marked development-only key is allowed in `appsettings.Development.json`; startup fails
  outside Development if no key is configured.
- **Authentication is required by default** through a fallback authorization policy; anonymous
  endpoints opt out explicitly with `[AllowAnonymous]`.
- The SPA stores the token in **`sessionStorage`**: it survives page reloads (important for a
  usable demo) and is cleared when the tab is closed.
- Registering an email that already exists returns **409 Conflict** (detailed in spec 001).
- Failed sign-ins are uniform: unknown email and wrong password return the same 401, and an
  unknown email is still checked against a dummy hash so response time does not reveal it.
- **Sign-out is client-side only**: the SPA discards the token; there is no server-side
  revocation list (spec 001, Clarification Q1).

## Alternatives considered

- **httpOnly cookie + SameSite**: not readable by JavaScript (better XSS posture), but requires
  CSRF protection and makes testing from API tools less direct.
- **Token in memory only**: smallest exposure, but every reload logs the user out.
- **`localStorage`**: persists across tabs and restarts, widening the exposure window.
- **Refresh tokens**: better UX for long sessions, but add storage, rotation, and revocation
  logic outside the scope of this exercise.
- **Uniform response for duplicate registration** (anti-enumeration): safer against account
  enumeration, but confusing for users when no email verification flow exists.

## Consequences

- `sessionStorage` is readable by scripts, so XSS must be prevented: React escapes output by
  default, `dangerouslySetInnerHTML` is not used, and no third-party scripts are loaded.
- Sessions end after 60 minutes; the SPA redirects to sign-in on 401.
- A token copied before sign-out stays valid until it expires (at most 60 minutes). Accepted
  risk, bounded by the short lifetime; revocation would need server-side session state.
- 409 on duplicate registration allows account enumeration; accepted and documented.
