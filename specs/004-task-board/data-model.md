# Data Model: Task Board

No persisted data changes. The Task entity and the API contract are those of spec 002 as amended by
spec 003. This document describes the client-side view state.

## BoardQuery (address state, `board-query.ts`)

| Field | Address parameter | Default | Rules |
|---|---|---|---|
| `search` | `search` | `''` | Trimmed; spec 002 FR-016 |
| `dueFrom`, `dueTo` | `dueFrom`, `dueTo` | `''` | Both or neither, valid `YYYY-MM-DD`, from ≤ to; otherwise dropped (spec 002 FR-023) |
| `sort` | `sort` | `dueDate` | `dueDate`, `createdAt`, `title`; otherwise default |
| `direction` | `direction` | `asc` (`desc` for `createdAt`) | `asc`, `desc`; otherwise the field's default |

- Defaults are left out of the address. `status`, `page` and `pageSize` are ignored when present.
- The same query string is carried on every board address (`/tasks`, `/tasks/new`, `/tasks/:id`,
  `/tasks/:id/edit`).

## Column (one per status)

| Field | Source |
|---|---|
| `status` | Fixed: `ToDo`, `InProgress`, `Done` (this order) |
| `label` | `STATUS_LABELS` ("To do", "In progress", "Done") |
| `styles` | `STATUS_STYLES` (top border, dot, header tint) |
| `tasks` | Concatenated items of the loaded pages |
| `totalCount` | `totalCount` of the first page response |
| `hasMore` | `page * pageSize < totalCount` of the last loaded page |

API request per column page: `GET /api/tasks?status=<status>&pageSize=20&page=<n>` plus the non-default
BoardQuery values (spec 002 contract).

## Panel state (route)

| Address | Content | Heading |
|---|---|---|
| `/tasks/new[?status=<status>]` | Create form; status preset from `status` when valid, else "To do" | "New task" |
| `/tasks/:id` | Task details with "Edit" and "Delete" | The task title |
| `/tasks/:id/edit` | Edit form | "Edit task" |

The `status` preset parameter is part of the create address only; it is not a board filter. Every
link or redirect back to the board rebuilds the query string from the board values
(`boardSearchFrom`), so the preset never reaches `/tasks`.
