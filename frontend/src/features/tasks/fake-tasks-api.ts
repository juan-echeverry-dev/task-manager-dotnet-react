import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import type { Task, TaskInput } from './tasks-api'

type Override = (url: URL, init: RequestInit | undefined) => Response | Promise<Response> | undefined

/**
 * Test-only stand-in for the Tasks API (contracts/tasks-api.md), backed by an in-memory list: the
 * board's per-status lists (with `search` and paging), get, create, update and delete. `override`
 * answers a request first when it returns a response.
 */
export function stubTasksApi(initial: Task[] = [], override?: Override) {
  const tasks = [...initial]
  let created = 0

  const fetchMock = stubFetch((rawUrl, init) => {
    const url = new URL(rawUrl, 'http://localhost')
    const answer = override?.(url, init)
    if (answer) return answer
    const method = init?.method ?? 'GET'

    if (url.pathname === '/api/tasks/settings') return jsonResponse({ dueDateRangeMaxDays: 90 })

    if (url.pathname === '/api/tasks' && method === 'GET') {
      const status = url.searchParams.get('status')
      const search = url.searchParams.get('search')?.toLowerCase()
      const page = Number(url.searchParams.get('page') ?? '1')
      const pageSize = Number(url.searchParams.get('pageSize') ?? '20')
      const matching = tasks.filter(
        (task) =>
          (!status || task.status === status) &&
          (!search || `${task.title} ${task.description ?? ''}`.toLowerCase().includes(search)),
      )
      const items = matching.slice((page - 1) * pageSize, page * pageSize)
      return jsonResponse({ items, page, pageSize, totalCount: matching.length })
    }

    if (url.pathname === '/api/tasks' && method === 'POST') {
      const input = JSON.parse(String(init?.body)) as TaskInput
      created += 1
      const task: Task = {
        id: `new-${created}`,
        ...input,
        createdAt: '2026-11-18T12:00:00+00:00',
        updatedAt: '2026-11-18T12:00:00+00:00',
      }
      tasks.push(task)
      return jsonResponse(task, 201)
    }

    const id = /^\/api\/tasks\/([^/]+)$/.exec(url.pathname)?.[1]
    const index = tasks.findIndex((task) => task.id === id)
    if (id === undefined) return new Response(null, { status: 404 })
    if (index < 0) return problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' })

    if (method === 'PUT') {
      const updated: Task = { ...(tasks[index] as Task), ...(JSON.parse(String(init?.body)) as TaskInput) }
      tasks[index] = updated
      return jsonResponse(updated)
    }
    if (method === 'DELETE') {
      tasks.splice(index, 1)
      return new Response(null, { status: 204 })
    }
    return jsonResponse(tasks[index])
  })

  return { fetchMock, tasks }
}

/** Requests sent with `method`, in order. */
export function requestsWith(fetchMock: ReturnType<typeof stubFetch>, method: string) {
  return fetchMock.mock.calls.filter(([, init]) => (init?.method ?? 'GET') === method)
}
