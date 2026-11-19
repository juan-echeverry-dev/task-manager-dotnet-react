import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, problemResponse, stubFetch, type FetchHandler } from '../../test/fetch'
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
    expect(screen.queryByRole('button', { name: 'Restore' })).not.toBeInTheDocument()
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

  describe('deleting', () => {
    function stubDelete(onDelete: FetchHandler = () => new Response(null, { status: 204 })) {
      return stubFetch((url, init) => {
        if (url === '/api/tasks/abc' && init?.method === 'DELETE') return onDelete(url, init)
        if (url === '/api/tasks/abc') return jsonResponse(task)
        if (url.startsWith('/api/tasks')) return jsonResponse({ items: [], page: 1, pageSize: 20, totalCount: 0 })
        return new Response(null, { status: 404 })
      })
    }

    async function openDialog() {
      const user = userEvent.setup()
      const result = renderApp('/tasks/abc')
      await user.click(await screen.findByRole('button', { name: 'Delete' }))
      return { ...result, user, dialog: screen.getByRole('dialog', { name: 'Delete task' }) }
    }

    it('asks for confirmation before deleting', async () => {
      const fetchMock = stubDelete()
      const { dialog } = await openDialog()

      expect(dialog).toHaveAccessibleDescription("Delete this task permanently? This can't be undone.")
      expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'DELETE')).toBe(false)
    })

    it('deletes on confirm and returns to the list with a confirmation', async () => {
      const fetchMock = stubDelete()
      const { user, dialog, router } = await openDialog()

      await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

      await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
      expect(await screen.findByText('Task deleted.')).toHaveAttribute('role', 'status')
      const init = fetchMock.mock.calls.find(([, requestInit]) => requestInit?.method === 'DELETE')?.[1]
      expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
    })

    it('changes nothing when cancelled', async () => {
      const fetchMock = stubDelete()
      const { user, router } = await openDialog()

      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/tasks/abc')
      expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'DELETE')).toBe(false)
    })

    it('disables the confirm action while deleting', async () => {
      stubDelete(() => new Promise<Response>(() => {}) as unknown as Response)
      const { user, dialog } = await openDialog()

      await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

      const confirm = await within(dialog).findByRole('button', { name: 'Deleting…' })
      expect(confirm).toBeDisabled()
      expect(confirm).toHaveAttribute('aria-busy', 'true')
    })

    it('shows the service message when deleting fails', async () => {
      stubDelete(() => Promise.reject(new TypeError('Failed to fetch')) as unknown as Response)
      const { user, dialog } = await openDialog()

      await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

      expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    })

    it('returns to the list with an alert when the task no longer exists', async () => {
      stubDelete(() => problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }))
      const { user, dialog, router } = await openDialog()

      await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

      await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
      expect(await screen.findByRole('alert')).toHaveTextContent('This task no longer exists.')
    })
  })

})
