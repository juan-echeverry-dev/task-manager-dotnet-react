import type { Task, TaskPage } from './tasks-api'

/** Builds a task as the API returns it (contracts/tasks-api.md). */
export function aTask(overrides: Partial<Task> = {}): Task {
  return {
    id: '0193a5c8-6f1e-7b2a-9c4d-2e8f1a3b5c7d',
    title: 'Prepare demo',
    description: null,
    status: 'ToDo',
    dueDate: null,
    createdAt: '2026-11-18T09:00:00+00:00',
    updatedAt: '2026-11-18T09:00:00+00:00',
    ...overrides,
  }
}

export function aPage(items: Task[], overrides: Partial<TaskPage> = {}): TaskPage {
  return { items, page: 1, pageSize: 20, totalCount: items.length, ...overrides }
}
