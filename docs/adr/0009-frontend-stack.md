# 0009. Frontend stack

- Status: Accepted
- Date: 2026-11-17

## Context

The requirements ask for a responsive, user-friendly SPA with cleanly organized components and
state, integrated with the API, ideally without browser console warnings.

## Decision

- **React 19 + TypeScript (strict, `noUncheckedIndexedAccess`) + Vite**.
- **React Router** for routing (route table in `src/app/routes.tsx`).
- **TanStack Query** for server state (caching, loading and error states); no global store.
  Session state (the access token) lives in a small React context backed by `sessionStorage`
  (ADR 0006), added with feature 001.
- **React Hook Form + Zod** for forms and client-side validation (added with feature 001).
- **Tailwind CSS** for responsive styling without a component library.
- **Vitest + Testing Library** for tests; **oxlint** (Vite's default linter) with warnings denied.
- Structure by feature: `src/app` (composition, routes), `src/features/<feature>`,
  `src/shared` (API client, layout, UI primitives).
- The Vite dev server proxies `/api` and `/health` to the backend, so the SPA and API share an
  origin in development and no CORS policy is required.

## Alternatives considered

- **Vue / Angular**: equally valid; React was chosen for ecosystem familiarity.
- **Redux / Zustand**: unnecessary when server state is handled by TanStack Query.
- **ESLint + typescript-eslint**: richer rule set, but the scaffold ships with TypeScript 7
  (native compiler) and oxlint; oxlint covers the needed rules with no extra configuration.
- **Component library (MUI, shadcn/ui)**: faster UI, but heavier and less transparent for a
  small surface.

## Consequences

- Clear separation between server state (Query) and UI state (component/local).
- Production deployment would need either the SPA served by the API or a CORS policy.
