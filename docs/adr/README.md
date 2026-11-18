# Architecture Decision Records

Significant decisions are recorded here using a lightweight [MADR](https://adr.github.io/madr/)
format: context, decision, alternatives considered, and consequences. ADRs are immutable once
accepted; a decision is changed by adding a new ADR that supersedes the old one.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-clean-architecture-with-vertical-slices.md) | Clean Architecture with vertical slices in four projects | Accepted |
| [0003](0003-identity-module-in-a-single-host.md) | Identity as a module of a single host ("second API") | Accepted |
| [0004](0004-persistence-ef-core-sqlite.md) | Persistence with EF Core and SQLite | Accepted |
| [0005](0005-password-hashing.md) | Password hashing with `PasswordHasher<T>` | Accepted |
| [0006](0006-jwt-bearer-authentication.md) | JWT bearer authentication | Accepted |
| [0007](0007-use-cases-without-mediator.md) | Use cases without a mediator, Result pattern, FluentValidation | Accepted |
| [0008](0008-testing-strategy.md) | Testing strategy and tooling | Accepted |
| [0009](0009-frontend-stack.md) | Frontend stack | Accepted |
| [0010](0010-http-api-conventions.md) | HTTP API conventions | Accepted |

Template for new ADRs:

```markdown
# NNNN. Title

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD

## Context
## Decision
## Alternatives considered
## Consequences
```
