import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { formatDate, formatTimestamp } from './format'
import { aTask } from './test-data'

const task = aTask({
  id: 'abc',
  title: 'Prepare demo',
  description: 'Slides and seed data',
  status: 'InProgress',
  dueDate: '2026-11-15',
  createdAt: '2026-11-10T09:00:00+00:00',
  updatedAt: '2026-11-12T15:30:00+00:00',
})

function stubTask(body: Response | (() => Response)) {
  return stubFetch((url) => (url === '/api/tasks/abc' ? (typeof body === 'function' ? body() : body) : new Response(null, { status: 404 })))
}

describe('TaskDetailPage', () => {
  beforeEach(() => {
    signedIn()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 18, 12, 0))
  })

  afterEach(() => {
    vi.useRealTimers()
    sessionStorage.clear()
  })

  it('shows every detail of the task', async () => {
    const fetchMock = stubTask(() => jsonResponse(task))
    renderApp('/tasks/abc')

    expect(await screen.findByRole('heading', { name: 'Prepare demo' })).toBeInTheDocument()
    expect(screen.getByText('Slides and seed data')).toBeInTheDocument()
    expect(screen.getByText('In progress')).toBeInTheDocument()
    expect(screen.getByText(formatDate('2026-11-15'))).toBeInTheDocument()
    expect(screen.getByText(formatTimestamp(task.createdAt))).toBeInTheDocument()
    expect(screen.getByText(formatTimestamp(task.updatedAt))).toBeInTheDocument()
    expect(screen.getByText('Overdue')).toBeInTheDocument()
    const init = fetchMock.mock.calls.find(([url]) => url === '/api/tasks/abc')?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
  })

  it('shows placeholders for a missing description and due date', async () => {
    stubTask(() => jsonResponse(aTask({ id: 'abc', description: null, dueDate: null })))
    renderApp('/tasks/abc')

    expect(await screen.findByText('No description')).toBeInTheDocument()
    expect(screen.getByText('No due date')).toBeInTheDocument()
    expect(screen.queryByText('Overdue')).not.toBeInTheDocument()
  })

  it('offers Edit and Delete for an active task', async () => {
    stubTask(() => jsonResponse(task))
    renderApp('/tasks/abc')

    expect(await screen.findByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/tasks/abc/edit')
    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
  })

  it('shows the status message passed by the previous screen', async () => {
    stubTask(() => jsonResponse(task))
    const { router } = renderApp('/tasks')
    await router.navigate('/tasks/abc', { state: { status: 'Task created.' } })

    // The layout's API indicator is also a status region, so find the message by its text.
    expect(await screen.findByText('Task created.')).toHaveAttribute('role', 'status')
  })

  it('shows "Task not found" with a link back to the list on 404', async () => {
    stubTask(() => problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }))
    renderApp('/tasks/abc')

    expect(await screen.findByRole('heading', { name: 'Task not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
  })

  it('shows the service message when the task cannot be loaded', async () => {
    stubFetch(() => Promise.reject(new TypeError('Failed to fetch')))
    renderApp('/tasks/abc')

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
  })
})
