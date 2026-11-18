# 0007. Use cases without a mediator, Result pattern, FluentValidation

- Status: Accepted
- Date: 2026-11-17

## Context

Each use case (slice) needs a handler, input validation, and a way to report expected failures
(validation errors, conflicts, not found) to the API without coupling Application to HTTP.

## Decision

- **Handlers are plain classes** (one per use case) registered in DI and injected directly into
  controllers. No mediator library.
- **Expected failures are values**: handlers return `Result`/`Result<T>` carrying a typed `Error`
  (`Validation`, `Conflict`, `NotFound`, `Unauthorized`). The Api maps them to Problem Details
  with the right status code. Exceptions are reserved for unexpected faults and are handled
  globally (`UseExceptionHandler` → 500 Problem Details, no internals leaked).
- **Input validation uses FluentValidation** (Apache-2.0) inside Application, invoked explicitly
  by each handler. No automatic ASP.NET Core validation pipeline, so rules stay testable and
  independent of HTTP. Domain invariants are still enforced by the Domain itself.
- The `Result` and `Error` types are introduced with the first feature that needs them
  (YAGNI), not in the foundation.

## Alternatives considered

- **MediatR**: popular, but commercially licensed since v13 and adds indirection (requests
  dispatched by type) that hides the call graph; cross-cutting behaviors are not needed yet.
- **Exceptions for control flow**: less explicit in signatures and more expensive.
- **Hand-written validators**: zero dependencies, but more boilerplate and a less familiar
  shape for reviewers; FluentValidation is the de facto standard in .NET.
- **Data annotations**: couple validation to transport models.

## Consequences

- Call graph is explicit (controller → handler) and navigable in the IDE.
- If cross-cutting concerns appear (logging, transactions), decorators can be added per handler
  without introducing a mediator.
