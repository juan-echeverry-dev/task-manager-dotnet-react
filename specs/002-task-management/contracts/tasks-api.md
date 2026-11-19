# Contract: Tasks HTTP API

> **Amended by [spec 003](../../003-permanent-task-deletion/spec.md)**: deletion is permanent; the deleted-tasks list and restore were removed.

Conventions:
- They follow ADR 0010 and feature 001.
- JSON bodies use camelCase.
- Errors are Problem Details (`application/problem+json`).
- Every endpoint requires `Authorization: Bearer <accessToken>`; without a valid session the
  response is 401.
- The owner is always the session's user. No request names an owner, and no response includes one.

**Ownership rule (FR-002)**: Any `{id}` that does not exist **or** belongs to another person
returns the same `404 Not Found` with `title: "Task not found"` and
`detail: "The task does not exist."`.

## `Task` (response body)

```json
{
  "id": "0193a5c8-6f1e-7b2a-9c4d-2e8f1a3b5c7d",
  "title": "Prepare demo",
  "description": "Slides and seed data",
  "status": "InProgress",
  "dueDate": "2026-12-01",
  "createdAt": "2026-11-18T09:00:00+00:00",
  "updatedAt": "2026-11-18T09:30:00+00:00",
  "deletedAt": null
}
```

`description`, `dueDate` and `deletedAt` may be `null`. `status` is `"ToDo"`, `"InProgress"` or
`"Done"`.

## `TaskPage` (response body of the lists)

```json
{ "items": [ /* Task */ ], "page": 1, "pageSize": 20, "totalCount": 25 }
```

## Endpoints

| Method and path | Purpose | Success | Errors |
|---|---|---|---|
| `GET /api/tasks` | Active tasks (list query below) | `200 TaskPage` | `400` invalid query |
| `GET /api/tasks/deleted` | Deleted tasks (same list query; extra sort `deletedAt`) | `200 TaskPage` | `400` invalid query |
| `GET /api/tasks/{id}` | One task, active or deleted | `200 Task` | `404` |
| `POST /api/tasks` | Create | `201 Task`, `Location: /api/tasks/{id}` | `400` |
| `PUT /api/tasks/{id}` | Replace title, description, status, due date | `200 Task` | `400`, `404`, `409` when deleted |
| `DELETE /api/tasks/{id}` | Soft delete (idempotent) | `204` | `404` |
| `POST /api/tasks/{id}/restore` | Restore (idempotent) | `200 Task` | `404` |
| `GET /api/tasks/settings` | Settings the screens need | `200 { "dueDateRangeMaxDays": 90 }` | — |

### Create — `POST /api/tasks`

```json
{ "title": "Prepare demo", "description": null, "status": null, "dueDate": "2026-12-01" }
```

Only `title` is required; `status` defaults to `"ToDo"`.

### Update — `PUT /api/tasks/{id}`

```json
{ "title": "Prepare demo", "description": "Slides", "status": "Done", "dueDate": null }
```

`title` and `status` are required. If `description` or `dueDate` is `null` or missing, the value is
cleared. When the task is deleted the response is
`409 { "title": "Task is deleted", "detail": "Restore the task before editing it." }`, and nothing
changes.

### List query (both lists)

| Parameter | Default | Values |
|---|---|---|
| `search` | — | Text, ≤ 200 chars; case-insensitive "contains" over title and description |
| `status` | — | `ToDo`, `InProgress`, `Done` |
| `dueFrom`, `dueTo` | — | `yyyy-MM-dd`; both or neither; inclusive; span ≤ configured maximum (90 days by default) |
| `sort` | `dueDate` (active) or `deletedAt` (deleted) | `dueDate`, `createdAt`, `title`; `deletedAt` only for `/deleted` |
| `direction` | Depends on `sort`: `asc` for `dueDate` and `title`; `desc` for `createdAt` and `deletedAt` | `asc`, `desc` |
| `page` | `1` | ≥ 1; past the last page → empty `items`, real `totalCount` |
| `pageSize` | `20` | 1–100 |

Ordering rules:
- Tasks without a due date always come last when sorting by `dueDate`.
- Ties are broken by `createdAt desc`, then `id`.
- With the range filter active, tasks without a due date are excluded.

400 responses are Validation Problem Details whose `errors` are keyed by the parameter names above,
for example `{ "errors": { "dueTo": ["Choose a range of at most 90 days."] } }`.
