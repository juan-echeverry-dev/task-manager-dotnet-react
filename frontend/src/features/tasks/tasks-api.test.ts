import { describe, expect, it } from 'vitest'
import { ApiError } from '../../shared/api/http-client'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { aPage, aTask } from './test-data'
import { createTask, listTasks } from './tasks-api'

describe('createTask', () => {
  it('posts the task as JSON with the bearer token and returns the created task', async () => {
    const task = aTask()
    const fetchMock = stubFetch(() => jsonResponse(task, 201))
    const input = { title: 'Prepare demo', description: null, status: 'ToDo' as const, dueDate: '2026-12-01' }

    await expect(createTask(input, 'token-123')).resolves.toEqual(task)

    const [url, init] = fetchMock.mock.calls[0] ?? []
    expect(url).toBe('/api/tasks')
    expect(init?.method).toBe('POST')
    const headers = new Headers(init?.headers)
    expect(headers.get('Authorization')).toBe('Bearer token-123')
    expect(headers.get('Content-Type')).toBe('application/json')
    expect(JSON.parse(String(init?.body))).toEqual(input)
  })

  it('surfaces a 400 as ApiError with the field errors', async () => {
    stubFetch(() => problemResponse(400, { errors: { title: ['Title is required.'] } }))

    const error = await createTask({ title: '', description: null, status: 'ToDo', dueDate: null }, 't').catch(
      (caught: unknown) => caught,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).problem?.errors?.title).toEqual(['Title is required.'])
  })
})

describe('listTasks', () => {
  it('gets the list with the bearer token and only the given query values', async () => {
    const page = aPage([aTask()])
    const fetchMock = stubFetch(() => jsonResponse(page))

    await expect(listTasks({ page: 2, pageSize: undefined }, 'token-123')).resolves.toEqual(page)

    const [url, init] = fetchMock.mock.calls[0] ?? []
    expect(url).toBe('/api/tasks?page=2')
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
  })

  it('omits the query string when there are no values', async () => {
    const fetchMock = stubFetch(() => jsonResponse(aPage([])))

    await listTasks({}, 'token-123')

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/tasks')
  })
})
