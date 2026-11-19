import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { formatTimestamp } from './format'
import { aPage, aTask } from './test-data'

const dueFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

function stubList(page = aPage([])) {
  return stubFetch((url) => (url.startsWith('/api/tasks') ? jsonResponse(page) : new Response(null, { status: 404 })))
}

describe('TaskListPage', () => {
  beforeEach(() => {
    signedIn()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 18, 12, 0))
  })

  afterEach(() => {
    vi.useRealTimers()
    sessionStorage.clear()
  })

  it('shows each task with its title link, status, due date and overdue badge', async () => {
    stubList(
      aPage([
        aTask({ id: 'a', title: 'Overdue task', status: 'InProgress', dueDate: '2026-11-15' }),
        aTask({ id: 'b', title: 'Done task', status: 'Done', dueDate: '2026-11-01' }),
        aTask({ id: 'c', title: 'Someday', status: 'ToDo', dueDate: null }),
      ]),
    )
    renderApp('/tasks')

    expect(await screen.findByRole('heading', { name: 'Tasks' })).toBeInTheDocument()
    const items = within(await screen.findByRole('list', { name: 'Tasks' })).getAllByRole('listitem')
    expect(items).toHaveLength(3)

    const [overdue, done, someday] = items as [HTMLElement, HTMLElement, HTMLElement]
    expect(within(overdue).getByRole('link', { name: 'Overdue task' })).toHaveAttribute('href', '/tasks/a')
    expect(within(overdue).getByText('In progress')).toBeInTheDocument()
    expect(within(overdue).getByText(dueFormat.format(new Date(2026, 10, 15)), { exact: false })).toBeInTheDocument()
    expect(within(overdue).getByText('Overdue')).toBeInTheDocument()

    expect(within(done).getByText('Done')).toBeInTheDocument()
    expect(within(done).queryByText('Overdue')).not.toBeInTheDocument()

    expect(within(someday).getByText('To do')).toBeInTheDocument()
    expect(within(someday).getByText('No due date')).toBeInTheDocument()
  })

  it('offers a link to create a task', async () => {
    stubList(aPage([aTask()]))
    renderApp('/tasks')

    expect(await screen.findByRole('link', { name: 'New task' })).toHaveAttribute('href', '/tasks/new')
  })

  it('shows an empty state with a link to create the first task', async () => {
    stubList(aPage([]))
    renderApp('/tasks')

    expect(await screen.findByText('You have no tasks yet.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create your first task' })).toHaveAttribute('href', '/tasks/new')
  })

  it('requests the page in the address and shows pagination', async () => {
    const fetchMock = stubList(aPage([aTask()], { page: 2, pageSize: 20, totalCount: 45 }))
    renderApp('/tasks?page=2')

    const nav = await screen.findByRole('navigation', { name: 'Pagination' })
    expect(fetchMock.mock.calls.some(([url]) => url === '/api/tasks?page=2')).toBe(true)
    expect(within(nav).getByRole('link', { name: 'Page 2' })).toHaveAttribute('aria-current', 'page')
    expect(within(nav).getByRole('link', { name: 'Page 1' })).toHaveAttribute('href', '/tasks')
    expect(within(nav).getByRole('link', { name: 'Page 3' })).toHaveAttribute('href', '/tasks?page=3')
    expect(within(nav).getByRole('link', { name: 'Previous page' })).toHaveAttribute('href', '/tasks')
    expect(within(nav).getByRole('link', { name: 'Next page' })).toHaveAttribute('href', '/tasks?page=3')
    expect(screen.getByText('45 tasks')).toBeInTheDocument()
  })

  it('hides pagination when everything fits on one page', async () => {
    stubList(aPage([aTask()]))
    renderApp('/tasks')

    await screen.findByRole('list', { name: 'Tasks' })
    expect(screen.queryByRole('navigation', { name: 'Pagination' })).not.toBeInTheDocument()
  })

  it('shows a message when the list cannot be loaded', async () => {
    stubFetch(() => Promise.reject(new TypeError('Failed to fetch')))
    renderApp('/tasks')

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
  })

  it('adds a Tasks link to the navigation when signed in', async () => {
    stubList()
    renderApp('/tasks')

    const nav = await screen.findByRole('navigation', { name: 'Account' })
    expect(within(nav).getByRole('link', { name: 'Tasks' })).toHaveAttribute('href', '/tasks')
  })

  it('links to the deleted tasks', async () => {
    stubList(aPage([aTask()]))
    renderApp('/tasks')

    expect(await screen.findByRole('link', { name: 'Deleted tasks' })).toHaveAttribute('href', '/tasks/deleted')
  })

  it('shows the status message passed by the previous screen', async () => {
    stubList(aPage([aTask()]))
    const { router } = renderApp('/profile')
    await router.navigate('/tasks', { state: { status: 'Task deleted.' } })

    expect(await screen.findByText('Task deleted.')).toHaveAttribute('role', 'status')
  })

  describe('deleted mode', () => {
    it('lists deleted tasks with their deletion date', async () => {
      const deletedAt = '2026-11-17T10:00:00+00:00'
      const fetchMock = stubList(aPage([aTask({ id: 'a', title: 'Old task', deletedAt, dueDate: '2026-11-01' })]))
      renderApp('/tasks/deleted')

      expect(await screen.findByRole('heading', { name: 'Deleted tasks' })).toBeInTheDocument()
      const list = await screen.findByRole('list', { name: 'Deleted tasks' })
      const [row] = within(list).getAllByRole('listitem') as [HTMLElement]
      expect(within(row).getByRole('link', { name: 'Old task' })).toHaveAttribute('href', '/tasks/a')
      expect(within(row).getByText(`Deleted ${formatTimestamp(deletedAt)}`)).toBeInTheDocument()
      expect(within(row).queryByText('Overdue')).not.toBeInTheDocument()
      expect(fetchMock.mock.calls.some(([url]) => url === '/api/tasks/deleted')).toBe(true)
      expect(screen.queryByRole('link', { name: 'New task' })).not.toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
    })

    it('shows its own empty state', async () => {
      stubList(aPage([]))
      renderApp('/tasks/deleted')

      expect(await screen.findByText('No deleted tasks.')).toBeInTheDocument()
      expect(screen.queryByRole('link', { name: 'Create your first task' })).not.toBeInTheDocument()
    })

    it('keeps its own address when paging', async () => {
      stubList(aPage([aTask()], { page: 1, pageSize: 20, totalCount: 30 }))
      renderApp('/tasks/deleted')

      const nav = await screen.findByRole('navigation', { name: 'Pagination' })
      expect(within(nav).getByRole('link', { name: 'Page 2' })).toHaveAttribute('href', '/tasks/deleted?page=2')
    })
  })
})
