import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { problemResponse } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { requestsWith, stubTasksApi } from './fake-tasks-api'
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

const openPanel = (name = 'Prepare demo') => screen.findByRole('dialog', { name })

describe('TaskDetailPanel', () => {
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
    const { fetchMock } = stubTasksApi([task])
    renderApp('/tasks/abc')

    const panel = await openPanel()
    expect(within(panel).getByRole('heading', { name: 'Prepare demo' })).toBeInTheDocument()
    expect(await within(panel).findByText('Slides and seed data')).toBeInTheDocument()
    expect(within(panel).getByText('In progress')).toBeInTheDocument()
    expect(within(panel).getByText(formatDate('2026-11-15'))).toBeInTheDocument()
    expect(within(panel).getByText(formatTimestamp(task.createdAt))).toBeInTheDocument()
    expect(within(panel).getByText(formatTimestamp(task.updatedAt))).toBeInTheDocument()
    expect(within(panel).getByText('Overdue')).toBeInTheDocument()
    const init = fetchMock.mock.calls.find(([url]) => url === '/api/tasks/abc')?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
  })

  it('shows placeholders for a missing description and due date', async () => {
    stubTasksApi([aTask({ id: 'abc', title: 'Prepare demo', description: null, dueDate: null })])
    renderApp('/tasks/abc')

    const panel = await openPanel()
    expect(await within(panel).findByText('No description')).toBeInTheDocument()
    expect(within(panel).getByText('No due date')).toBeInTheDocument()
    expect(within(panel).queryByText('Overdue')).not.toBeInTheDocument()
  })

  it('offers Edit, keeping the board query string, and Delete', async () => {
    stubTasksApi([task])
    renderApp('/tasks/abc?search=demo')

    const panel = await openPanel()
    expect(await within(panel).findByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/tasks/abc/edit?search=demo')
    expect(within(panel).getByRole('button', { name: 'Delete' })).toBeInTheDocument()
  })

  it('shows the status message passed by the previous screen inside the panel only', async () => {
    stubTasksApi([task])
    const { router } = renderApp('/tasks')
    await router.navigate('/tasks/abc', { state: { status: 'Task created.' } })

    const panel = await openPanel()
    expect(await within(panel).findByText('Task created.')).toHaveAttribute('role', 'status')
    expect(screen.getAllByText('Task created.')).toHaveLength(1)
  })

  it('shows the service message when the task cannot be loaded', async () => {
    stubTasksApi([task], (url) =>
      url.pathname === '/api/tasks/abc' ? (Promise.reject(new TypeError('Failed to fetch')) as unknown as Response) : undefined,
    )
    renderApp('/tasks/abc')

    const panel = await screen.findByRole('dialog')
    expect(await within(panel).findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
  })

  describe('deleting', () => {
    async function openConfirmation(path = '/tasks/abc') {
      const user = userEvent.setup()
      const result = renderApp(path)
      await user.click(await within(await openPanel()).findByRole('button', { name: 'Delete' }))
      return { ...result, user, confirmation: screen.getByRole('dialog', { name: 'Delete task' }) }
    }

    it('asks for the permanent-deletion confirmation', async () => {
      const { fetchMock } = stubTasksApi([task])
      const { confirmation } = await openConfirmation()

      expect(confirmation).toHaveAccessibleDescription("Delete this task permanently? This can't be undone.")
      expect(requestsWith(fetchMock, 'DELETE')).toHaveLength(0)
    })

    it('deletes on confirm, closes the panel and shows the confirmation on the board', async () => {
      const { fetchMock } = stubTasksApi([task])
      const { user, confirmation, router } = await openConfirmation('/tasks/abc?search=demo')

      await user.click(within(confirmation).getByRole('button', { name: 'Delete' }))

      await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
      expect(router.state.location.search).toBe('?search=demo')
      expect(await screen.findByText('Task deleted.')).toHaveAttribute('role', 'status')
      expect(screen.queryByRole('dialog', { name: 'Prepare demo' })).not.toBeInTheDocument()
      const init = requestsWith(fetchMock, 'DELETE')[0]?.[1]
      expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
    })

    it('changes nothing when cancelled', async () => {
      const { fetchMock } = stubTasksApi([task])
      const { user, router } = await openConfirmation()

      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(screen.queryByRole('dialog', { name: 'Delete task' })).not.toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/tasks/abc')
      expect(requestsWith(fetchMock, 'DELETE')).toHaveLength(0)
    })

    it('disables the confirm action while deleting', async () => {
      stubTasksApi([task], (_url, init) =>
        init?.method === 'DELETE' ? (new Promise<Response>(() => {}) as unknown as Response) : undefined,
      )
      const { user, confirmation } = await openConfirmation()

      await user.click(within(confirmation).getByRole('button', { name: 'Delete' }))

      const confirm = await within(confirmation).findByRole('button', { name: 'Deleting…' })
      expect(confirm).toBeDisabled()
      expect(confirm).toHaveAttribute('aria-busy', 'true')
    })

    it('shows the service message when deleting fails', async () => {
      stubTasksApi([task], (_url, init) =>
        init?.method === 'DELETE' ? (Promise.reject(new TypeError('Failed to fetch')) as unknown as Response) : undefined,
      )
      const { user, confirmation } = await openConfirmation()

      await user.click(within(confirmation).getByRole('button', { name: 'Delete' }))

      expect(await within(await openPanel()).findByRole('alert')).toHaveTextContent(
        "We couldn't reach the service. Please try again.",
      )
    })

    it('returns to the board with an alert when the task no longer exists', async () => {
      stubTasksApi([task], (_url, init) =>
        init?.method === 'DELETE' ? problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }) : undefined,
      )
      const { user, confirmation, router } = await openConfirmation()

      await user.click(within(confirmation).getByRole('button', { name: 'Delete' }))

      await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
      expect(await screen.findByText('This task no longer exists.')).toHaveAttribute('role', 'alert')
    })
  })
})
