# 0001. Record architecture decisions

- Status: Accepted
- Date: 2026-11-17

## Context

The project is built with Spec-Driven Development (GitHub Spec Kit). Specs capture *what* and
*why* for features; they deliberately do not capture cross-cutting technical choices. Those
choices still need a durable, reviewable record, especially because the code base will be
reviewed live and every decision must be explainable.

## Decision

Record every significant architectural or technological decision as an ADR in `docs/adr/`.
The constitution (`.specify/memory/constitution.md`) holds principles; ADRs hold concrete
choices; feature `plan.md` files reference ADRs instead of repeating them.

## Alternatives considered

- **Decisions only inside `plan.md`**: scattered across features and hard to find later.
- **Wiki or external documents**: drift from the code and are not reviewed in pull requests.

## Consequences

- Reviewers can trace why a library or pattern exists.
- Changing a decision requires a new ADR, which makes reversals explicit.
