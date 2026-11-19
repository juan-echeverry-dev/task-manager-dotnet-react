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

      expect(dialog).toHaveAccessibleDescription('Delete this task? You can restore it from Deleted tasks.')
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
  })

  describe('a deleted task', () => {
    const deleted = { ...task, deletedAt: '2026-11-17T10:00:00+00:00' }

    function stubDeletedTask(onRestore: FetchHandler = () => jsonResponse({ ...task })) {
      let restored = false
      return stubFetch((url, init) => {
        if (url === '/api/tasks/abc/restore' && init?.method === 'POST') {
          restored = true
          return onRestore(url, init)
        }
        if (url === '/api/tasks/abc') return jsonResponse(restored ? task : deleted)
        return new Response(null, { status: 404 })
      })
    }

    it('is read-only, marked as deleted, with Restore as the only action', async () => {
      stubDeletedTask()
      renderApp('/tasks/abc')

      expect(await screen.findByText(`Deleted on ${formatTimestamp(deleted.deletedAt)}`)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Restore' })).toBeInTheDocument()
      expect(screen.queryByRole('link', { name: 'Edit' })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
      expect(screen.queryByText('Overdue')).not.toBeInTheDocument()
    })

    it('can be restored and then shows the active detail with a confirmation', async () => {
      const fetchMock = stubDeletedTask()
      const user = userEvent.setup()
      renderApp('/tasks/abc')

      await user.click(await screen.findByRole('button', { name: 'Restore' }))

      expect(await screen.findByText('Task restored.')).toHaveAttribute('role', 'status')
      expect(await screen.findByRole('link', { name: 'Edit' })).toBeInTheDocument()
      expect(screen.queryByText(/^Deleted on/)).not.toBeInTheDocument()
      const init = fetchMock.mock.calls.find(([url]) => url === '/api/tasks/abc/restore')?.[1]
      expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
    })

    it('disables Restore while restoring', async () => {
      stubDeletedTask(() => new Promise<Response>(() => {}) as unknown as Response)
      const user = userEvent.setup()
      renderApp('/tasks/abc')

      await user.click(await screen.findByRole('button', { name: 'Restore' }))

      const button = await screen.findByRole('button', { name: 'Restoring…' })
      expect(button).toBeDisabled()
      expect(button).toHaveAttribute('aria-busy', 'true')
    })

    it('shows the alert passed by an edit that was refused', async () => {
      stubDeletedTask()
      const { router } = renderApp('/tasks')
      await router.navigate('/tasks/abc', { state: { alert: 'This task was deleted. Restore it to edit.' } })

      expect(await screen.findByText('This task was deleted. Restore it to edit.')).toHaveAttribute('role', 'alert')
    })
  })
})
