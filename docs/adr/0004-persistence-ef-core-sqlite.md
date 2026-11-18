# 0004. Persistence with EF Core and SQLite

- Status: Accepted
- Date: 2026-11-17

## Context

We need a relational store for users and business data, runnable by any reviewer from a fresh
clone without installing a database server or Docker.

## Decision

- **SQLite** as the database (`Data Source=tasklane.db`, configurable via
  `ConnectionStrings:Default`).
- **EF Core 10** as the data access technology, with **versioned migrations** in
  `Infrastructure/Persistence/Migrations` and entity configurations in
  `Infrastructure/Persistence/Configurations`.
- Application declares **aggregate-specific repository ports** (e.g. `IUserRepository` with
  intention-revealing methods such as `ExistsByEmailAsync`), implemented in Infrastructure.
  No generic repository and no Unit-of-Work wrapper: `DbContext` already is one.
- Migrations are applied automatically on startup **only in Development**; demo data is seeded
  the same way.

## Alternatives considered

- **SQL Server / PostgreSQL**: closer to production, but require a server or Docker to run.
  EF Core keeps the switch to another provider cheap.
- **Dapper**: more control and speed, but more hand-written SQL and mapping for no benefit here.
- **Generic `IRepository<T>`**: leaks query concerns into Application or degenerates into a
  `DbSet` wrapper; forbidden by constitution principle V.
- **EF Core InMemory provider for tests**: not relational (no constraints, no transactions);
  tests use real SQLite instead (see ADR 0008).

## Consequences

- Zero-install local runs; the database file is git-ignored.
- SQLite limitations to keep in mind: limited `ALTER TABLE` support in migrations,
  `DateTimeOffset` ordering requires a value converter, case sensitivity of `LIKE`/collations.
