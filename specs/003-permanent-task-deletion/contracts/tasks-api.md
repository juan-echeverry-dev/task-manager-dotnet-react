# Contract delta: Tasks API

Changes to [spec 002 tasks-api.md](../../002-task-management/contracts/tasks-api.md). Everything not
listed is unchanged.

## Task resource

The `deletedAt` property is **removed**:

```json
{
  "id": "0193…",
  "title": "Prepare demo",
  "description": null,
  "status": "ToDo",
  "dueDate": "2026-12-01",
  "createdAt": "2026-11-19T10:00:00+00:00",
  "updatedAt": "2026-11-19T10:00:00+00:00"
}
```

## Endpoints

| Endpoint | Before (spec 002) | After |
|---|---|---|
| `GET /api/tasks` | Active tasks | All of the person's tasks; `sort` accepts `dueDate`, `createdAt`, `title` |
| `GET /api/tasks/deleted` | Deleted tasks | **Removed**: no route matches, `404` |
| `GET /api/tasks/{id}` | Active or deleted task; `404` | Existing task; `404` when missing, deleted, or foreign |
| `PUT /api/tasks/{id}` | `400`, `404`, `409` when deleted | `400`, `404` (also when the task is deleted during the request). **No `409`** |
| `DELETE /api/tasks/{id}` | Soft delete, idempotent `204` | **Permanent** delete: `204` the first time; `404` when missing, already deleted, or foreign |
| `POST /api/tasks/{id}/restore` | Restore | **Removed**: no route matches, `404` |
| `GET /api/tasks/settings`, `POST /api/tasks` | — | Unchanged |

Errors stay RFC 9457 Problem Details; the `404` body for a task is
`{ "title": "Task not found", "detail": "The task does not exist." }`.

Note: `DELETE` is no longer idempotent in its status code (a second call returns `404`); the
resulting state is the same, so retrying a delete is still safe.
