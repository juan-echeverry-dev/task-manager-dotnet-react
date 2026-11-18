# 0003. Identity as a module of a single host ("second API")

- Status: Accepted
- Date: 2026-11-17

## Context

The requirements ask for "a second API" with user creation, login, and authorized and
non-authorized endpoints, in addition to the business CRUD API. This can be read as a second
deployable service or as a second, separate API surface.

## Decision

Implement user management as an **Identity module inside the same ASP.NET Core host**, exposed
under its own routes (`/api/auth/*`, `/api/users/*`) with its own slices in every layer. Business
modules depend on Identity only through the authenticated user's id claim, never on its
internals. Module boundaries are verified by architecture tests once a second module exists.

## Alternatives considered

- **Two hosts** (an Identity API issuing tokens and a business API validating them): the most
  literal reading and independently deployable, but it requires sharing signing keys, two
  processes, CORS/two base URLs in the frontend, and loses a real foreign key to the users table.
  That cost is not justified at this scale.
- **Two controllers without module boundaries**: simplest, but nothing prevents coupling between
  user management and business features.

## Consequences

- One process, one database, real referential integrity, one origin for the frontend.
- The module can be extracted into its own service later because its boundary is explicit:
  only the token contract (issuer, audience, `sub` claim) would need to be shared.
