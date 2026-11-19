import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { aPage, aTask } from './test-data'

const dueFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

function stubList(page = aPage([])) {
  return stubFetch((url) => {
    if (url === '/api/tasks/settings') return jsonResponse({ dueDateRangeMaxDays: 90 })
    return url.startsWith('/api/tasks') ? jsonResponse(page) : new Response(null, { status: 404 })
  })
}

function listCalls(fetchMock: ReturnType<typeof stubFetch>) {
  return fetchMock.mock.calls.map(([url]) => String(url)).filter((url) => url.startsWith('/api/tasks') && url !== '/api/tasks/settings')
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

  it('shows an empty page beyond the last one, for example after deleting its last task', async () => {
    stubList(aPage([], { page: 3, pageSize: 20, totalCount: 40 }))
    renderApp('/tasks?page=3')

    expect(await screen.findByText('This page is empty.')).toBeInTheDocument()
    expect(screen.getByText('40 tasks')).toBeInTheDocument()
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

  it('offers no deleted-tasks area', async () => {
    stubList(aPage([aTask()]))
    renderApp('/tasks')

    await screen.findByRole('list', { name: 'Tasks' })
    expect(screen.queryByRole('link', { name: 'Deleted tasks' })).not.toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Deleted on' })).not.toBeInTheDocument()
  })

  it('shows the status message passed by the previous screen', async () => {
    stubList(aPage([aTask()]))
    const { router } = renderApp('/profile')
    await router.navigate('/tasks', { state: { status: 'Task deleted.' } })

    expect(await screen.findByText('Task deleted.')).toHaveAttribute('role', 'status')
  })

  describe('finding tasks', () => {
    it('requests the list described by the address', async () => {
      const fetchMock = stubList(aPage([aTask()]))
      renderApp('/tasks?search=demo&status=Done&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc&page=2')

      await screen.findByRole('list', { name: 'Tasks' })
      expect(listCalls(fetchMock)).toContain(
        '/api/tasks?search=demo&status=Done&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc&page=2',
      )
      expect(screen.getByLabelText('Search')).toHaveValue('demo')
      expect(screen.getByLabelText('Status')).toHaveValue('Done')
    })

    it('ignores invalid values in the address', async () => {
      const fetchMock = stubList(aPage([aTask()]))
      renderApp('/tasks?status=Later&sort=priority&page=abc')

      await screen.findByRole('list', { name: 'Tasks' })
      expect(listCalls(fetchMock)).toEqual(['/api/tasks'])
    })

    it('shows the configured maximum range from the settings', async () => {
      stubList(aPage([aTask()]))
      renderApp('/tasks')

      await waitFor(() => expect(screen.getByLabelText('Due to')).toHaveAccessibleDescription('Up to 90 days.'))
    })

    it('updates the address after the person stops typing a search', async () => {
      vi.useRealTimers()
      stubList(aPage([aTask()]))
      const { router } = renderApp('/tasks?page=3')
      const user = userEvent.setup()

      await user.type(await screen.findByLabelText('Search'), 'demo')

      expect(router.state.location.search).toBe('?page=3')
      await waitFor(() => expect(router.state.location.search).toBe('?search=demo'), { timeout: 2000 })
    })

    it('applies a status filter to the address and resets the page', async () => {
      stubList(aPage([aTask()]))
      const { router } = renderApp('/tasks?page=3')
      const user = userEvent.setup()

      await user.selectOptions(await screen.findByLabelText('Status'), 'Done')

      expect(router.state.location.search).toBe('?status=Done')
    })

    it('keeps the search and filters in the pagination links', async () => {
      stubList(aPage([aTask()], { page: 1, pageSize: 20, totalCount: 30 }))
      renderApp('/tasks?search=demo')

      const nav = await screen.findByRole('navigation', { name: 'Pagination' })
      expect(within(nav).getByRole('link', { name: 'Page 2' })).toHaveAttribute('href', '/tasks?search=demo&page=2')
    })

    it('distinguishes "no match" from "no tasks" and offers to clear the search', async () => {
      stubList(aPage([]))
      const { router } = renderApp('/tasks?search=zzz&sort=title')
      const user = userEvent.setup()

      expect(await screen.findByText('No tasks match your search.')).toBeInTheDocument()
      expect(screen.queryByText('You have no tasks yet.')).not.toBeInTheDocument()

      const clearButtons = screen.getAllByRole('button', { name: 'Clear search and filters' })
      await user.click(clearButtons.at(-1) as HTMLElement)

      expect(router.state.location.search).toBe('?sort=title')
    })

    it('shows the reason when the API refuses the query', async () => {
      stubFetch((url) => {
        if (url === '/api/tasks/settings') return jsonResponse({ dueDateRangeMaxDays: 30 })
        return problemResponse(400, { errors: { dueTo: ['Choose a range of at most 30 days.'] } })
      })
      renderApp('/tasks?dueFrom=2026-01-01&dueTo=2026-03-01')

      expect(await screen.findByRole('alert')).toHaveTextContent('Choose a range of at most 30 days.')
    })
  })
})
