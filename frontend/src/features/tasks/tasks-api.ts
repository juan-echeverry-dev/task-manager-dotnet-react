import { request } from '../../shared/api/http-client'

/** Task statuses, by their API names (contracts/tasks-api.md). */
export const TASK_STATUSES = ['ToDo', 'InProgress', 'Done'] as const
export type TaskStatus = (typeof TASK_STATUSES)[number]

/** A task as returned by the API. Dates are `yyyy-MM-dd`; timestamps are ISO 8601. */
export interface Task {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
  createdAt: string
  updatedAt: string
  deletedAt: string | null
}

export interface TaskPage {
  items: Task[]
  page: number
  pageSize: number
  totalCount: number
}

export interface TaskInput {
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
}

/** List query values, named as in the API; undefined values are left out of the request. */
export type ListTasksParams = Record<string, string | number | undefined>

export function createTask(input: TaskInput, accessToken: string): Promise<Task> {
  return request<Task>(
    '/api/tasks',
    { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) },
    { accessToken },
  )
}

export function getTask(id: string, accessToken: string): Promise<Task> {
  return request<Task>(`/api/tasks/${encodeURIComponent(id)}`, {}, { accessToken })
}

export function updateTask(id: string, input: TaskInput, accessToken: string): Promise<Task> {
  return request<Task>(
    `/api/tasks/${encodeURIComponent(id)}`,
    { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) },
    { accessToken },
  )
}

export function deleteTask(id: string, accessToken: string): Promise<void> {
  return request<void>(`/api/tasks/${encodeURIComponent(id)}`, { method: 'DELETE' }, { accessToken })
}

export function restoreTask(id: string, accessToken: string): Promise<Task> {
  return request<Task>(`/api/tasks/${encodeURIComponent(id)}/restore`, { method: 'POST' }, { accessToken })
}

export function listTasks(params: ListTasksParams, accessToken: string): Promise<TaskPage> {
  return request<TaskPage>(withQuery('/api/tasks', params), {}, { accessToken })
}

export function listDeletedTasks(params: ListTasksParams, accessToken: string): Promise<TaskPage> {
  return request<TaskPage>(withQuery('/api/tasks/deleted', params), {}, { accessToken })
}

function withQuery(path: string, params: ListTasksParams): string {
  const search = new URLSearchParams()
  for (const [name, value] of Object.entries(params)) {
    if (value !== undefined) search.set(name, String(value))
  }
  const query = search.toString()
  return query ? `${path}?${query}` : path
}
