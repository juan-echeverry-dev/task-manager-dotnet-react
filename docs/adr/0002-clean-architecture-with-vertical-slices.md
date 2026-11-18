# 0002. Clean Architecture with vertical slices in four projects

- Status: Accepted
- Date: 2026-11-17

## Context

The requirements ask for a data access layer, a business logic layer that is *independent of the
data layer and the API*, and an API layer, all following Clean Architecture. We also want each
feature to be cohesive and easy to add without touching unrelated code.

## Decision

Four production projects with inward-only references:

```
TaskLane.Api ──► TaskLane.Infrastructure ──► TaskLane.Application ──► TaskLane.Domain
     └──────────────────────────────────────────────▲
```

| Project | Responsibility | May reference |
|---|---|---|
| `Domain` | Entities, value objects, invariants | nothing |
| `Application` | Use cases (business rules, validation), ports it owns | `Domain` |
| `Infrastructure` | Adapters: EF Core/SQLite, hashing, token issuing | `Application` |
| `Api` | HTTP transport, auth policies, composition root | `Application`, `Infrastructure` |

Inside each project, code is organized **by module and use case** (vertical slices), e.g.
`Application/Identity/RegisterUser/{Command, Handler, Validator, Response}`.

Enforcement:

1. The project graph: an outward reference would create a circular dependency and fail the build.
2. Architecture tests (`TaskLane.ArchitectureTests`): type-level layer rules (ArchUnitNET) and a
   ban on EF Core, SQLite, and ASP.NET Core assemblies in `Domain` and `Application`.

## Alternatives considered

- **Pure Vertical Slice Architecture** (handlers use `DbContext` directly, one project): less
  ceremony, but business logic would depend on the data layer, contradicting the requirement.
- **One project per module and layer** (modular monolith with ~8 projects): stronger isolation,
  but unjustified with a single module today. Modules are folders whose boundaries are covered
  by architecture tests; they can be promoted to projects later.
- **Single project with folders**: dependency rules would rely on discipline only.

## Consequences

- The compiler and tests guard the dependency rule; violations fail CI.
- Adding a feature means adding a slice folder in each relevant layer, not editing shared files
  (except DI registration and the route table).
- Application defines ports (interfaces) for what it needs from the outside, which adds a small
  number of interfaces — only at real boundaries (see ADR 0007 and constitution principle V).
