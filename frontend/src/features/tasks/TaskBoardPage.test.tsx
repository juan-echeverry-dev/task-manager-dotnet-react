import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import type { Task, TaskStatus } from './tasks-api'
import { aPage, aTask } from './test-data'

const dueFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

type Columns = Partial<Record<TaskStatus, Task[]>>

/** Answers each column with its tasks, 20 per page, as the API would (status, page, pageSize). */
function stubBoard(columns: Columns = {}, override?: (url: URL) => Response | Promise<Response> | undefined) {
  return stubFetch((url) => {
    if (url === '/api/tasks/settings') return jsonResponse({ dueDateRangeMaxDays: 90 })
    if (!url.startsWith('/api/tasks?')) return new Response(null, { status: 404 })
    const parsed = new URL(url, 'http://localhost')
    const answer = override?.(parsed)
    if (answer) return answer
    const status = parsed.searchParams.get('status') as TaskStatus
    const page = Number(parsed.searchParams.get('page') ?? '1')
    const pageSize = Number(parsed.searchParams.get('pageSize') ?? '20')
    const all = columns[status] ?? []
    return jsonResponse(aPage(all.slice((page - 1) * pageSize, page * pageSize), { page, pageSize, totalCount: all.length }))
  })
}

function columnCalls(fetchMock: ReturnType<typeof stubFetch>) {
  return fetchMock.mock.calls
    .map(([url]) => String(url))
    .filter((url) => url.startsWith('/api/tasks?'))
    .map((url) => new URL(url, 'http://localhost').searchParams)
}

function column(name: string) {
  return screen.getByRole('region', { name })
}

const tasksOf = (status: TaskStatus, count: number, prefix = status) =>
  Array.from({ length: count }, (_, index) => aTask({ id: `${prefix}-${index + 1}`, title: `${prefix} ${index + 1}`, status }))

describe('TaskBoardPage', () => {
  beforeEach(() => {
    signedIn()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 18, 12, 0))
  })

  afterEach(() => {
    vi.useRealTimers()
    sessionStorage.clear()
  })

  it('shows three columns in status order, each with only its tasks and its count', async () => {
    stubBoard({
      ToDo: [aTask({ id: 'a', title: 'Write notes', status: 'ToDo' }), aTask({ id: 'b', title: 'Book room', status: 'ToDo' })],
      InProgress: [aTask({ id: 'c', title: 'Prepare demo', status: 'InProgress' })],
      Done: [],
    })
    renderApp('/tasks')

    expect(await screen.findByRole('heading', { level: 1, name: 'Tasks' })).toBeInTheDocument()
    const lists = await screen.findAllByRole('list', { name: /^(To do|In progress|Done)$/ })
    expect(lists.map((list) => list.getAttribute('aria-label'))).toEqual(['To do', 'In progress', 'Done'])

    await waitFor(() => expect(within(column('To do')).getByText('2 tasks')).toBeInTheDocument())
    expect(within(screen.getByRole('list', { name: 'To do' })).getAllByRole('listitem')).toHaveLength(2)
    expect(within(column('In progress')).getByText('1 task')).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'In progress' })).getByText('Prepare demo')).toBeInTheDocument()
    expect(within(column('Done')).getByText('0 tasks')).toBeInTheDocument()
    expect(within(column('Done')).getByText('No tasks')).toBeInTheDocument()
  })

  it('asks the API for each status, one batch of 20 at a time', async () => {
    const fetchMock = stubBoard()
    renderApp('/tasks')

    await waitFor(() => expect(columnCalls(fetchMock)).toHaveLength(3))
    const calls = columnCalls(fetchMock)
    expect(calls.map((params) => params.get('status')).sort()).toEqual(['Done', 'InProgress', 'ToDo'])
    expect(calls.every((params) => params.get('pageSize') === '20' && !params.has('page'))).toBe(true)
  })

  it('shows on each card the title link, a description excerpt, the due date and the overdue marker', async () => {
    stubBoard({
      InProgress: [
        aTask({ id: 'a', title: 'Overdue task', status: 'InProgress', description: 'Slides and seed data', dueDate: '2026-11-15' }),
      ],
      Done: [aTask({ id: 'b', title: 'Done task', status: 'Done', dueDate: '2026-11-01' })],
      ToDo: [aTask({ id: 'c', title: 'Someday', status: 'ToDo', dueDate: null })],
    })
    renderApp('/tasks?search=task')

    const overdue = await within(await screen.findByRole('list', { name: 'In progress' })).findByRole('listitem')
    expect(within(overdue).getByRole('link', { name: 'Overdue task' })).toHaveAttribute('href', '/tasks/a?search=task')
    expect(within(overdue).getByText('Slides and seed data')).toBeInTheDocument()
    expect(within(overdue).getByText(dueFormat.format(new Date(2026, 10, 15)), { exact: false })).toBeInTheDocument()
    expect(within(overdue).getByText('Overdue')).toBeInTheDocument()

    const done = await within(await screen.findByRole('list', { name: 'Done' })).findByRole('listitem')
    expect(within(done).queryByText('Overdue')).not.toBeInTheDocument()

    const someday = await within(await screen.findByRole('list', { name: 'To do' })).findByRole('listitem')
    expect(within(someday).getByText('No due date')).toBeInTheDocument()
  })

  it('loads more tasks in one column without reloading the others', async () => {
    const fetchMock = stubBoard({ ToDo: tasksOf('ToDo', 25), Done: tasksOf('Done', 1) })
    renderApp('/tasks')
    const user = userEvent.setup()

    const list = await screen.findByRole('list', { name: 'To do' })
    await waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(20))
    expect(within(column('To do')).getByText('25 tasks')).toBeInTheDocument()
    expect(within(column('Done')).queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()

    await user.click(within(column('To do')).getByRole('button', { name: 'Load more' }))

    await waitFor(() => expect(within(list).getAllByRole('listitem')).toHaveLength(25))
    expect(within(list).getByText('ToDo 25')).toBeInTheDocument()
    expect(within(column('To do')).queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
    const pageTwo = columnCalls(fetchMock).filter((params) => params.get('page') === '2')
    expect(pageTwo.map((params) => params.get('status'))).toEqual(['ToDo'])
    expect(columnCalls(fetchMock).filter((params) => params.get('status') === 'Done')).toHaveLength(1)
  })

  it('disables "Load more" while the next batch loads', async () => {
    stubBoard({ ToDo: tasksOf('ToDo', 21) }, (url) =>
      url.searchParams.get('page') === '2' ? (new Promise<Response>(() => {}) as unknown as Response) : undefined,
    )
    renderApp('/tasks')
    const user = userEvent.setup()

    await user.click(await within(await screen.findByRole('region', { name: 'To do' })).findByRole('button', { name: 'Load more' }))

    const button = await within(column('To do')).findByRole('button', { name: 'Loading more…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('shows a loading message in each column while it loads', async () => {
    stubBoard({}, () => new Promise<Response>(() => {}) as unknown as Response)
    renderApp('/tasks')

    expect(await within(await screen.findByRole('region', { name: 'In progress' })).findByText('Loading tasks…')).toBeInTheDocument()
  })

  it('invites to create the first task when there are no tasks at all', async () => {
    stubBoard()
    renderApp('/tasks')

    expect(await screen.findByText('You have no tasks yet.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create your first task' })).toHaveAttribute('href', '/tasks/new')
  })

  it('distinguishes "no match" from "no tasks" and offers to clear the search', async () => {
    stubBoard()
    const { router } = renderApp('/tasks?search=zzz&sort=title')
    const user = userEvent.setup()

    expect(await screen.findByText('No tasks match your search.')).toBeInTheDocument()
    expect(screen.queryByText('You have no tasks yet.')).not.toBeInTheDocument()

    const clearButtons = screen.getAllByRole('button', { name: 'Clear search and filters' })
    await user.click(clearButtons.at(-1) as HTMLElement)

    expect(router.state.location.search).toBe('?sort=title')
  })

  it('shows the service message in a column that fails while the others still render', async () => {
    stubBoard({ Done: [aTask({ id: 'd', title: 'Shipped', status: 'Done' })] }, (url) =>
      url.searchParams.get('status') === 'ToDo' ? (Promise.reject(new TypeError('Failed to fetch')) as unknown as Response) : undefined,
    )
    renderApp('/tasks')

    expect(await within(await screen.findByRole('region', { name: 'To do' })).findByRole('alert')).toHaveTextContent(
      "We couldn't reach the service. Please try again.",
    )
    expect(await within(column('Done')).findByText('Shipped')).toBeInTheDocument()
  })

  it('shows the reason when the API refuses the query', async () => {
    stubBoard({}, () => problemResponse(400, { errors: { dueTo: ['Choose a range of at most 30 days.'] } }))
    renderApp('/tasks?dueFrom=2026-01-01&dueTo=2026-03-01')

    expect((await screen.findAllByText('Choose a range of at most 30 days.')).length).toBeGreaterThan(0)
  })

  it('offers a "New task" link that keeps the board query string', async () => {
    stubBoard({ ToDo: tasksOf('ToDo', 1) })
    renderApp('/tasks?search=demo')

    expect(await screen.findByRole('link', { name: 'New task' })).toHaveAttribute('href', '/tasks/new?search=demo')
  })

  it('shows the status message passed by the previous screen', async () => {
    stubBoard({ ToDo: tasksOf('ToDo', 1) })
    const { router } = renderApp('/profile')
    await router.navigate('/tasks', { state: { status: 'Task deleted.' } })

    expect(await screen.findByText('Task deleted.')).toHaveAttribute('role', 'status')
  })

  it('adds a Tasks link to the navigation when signed in', async () => {
    stubBoard()
    renderApp('/tasks')

    const nav = await screen.findByRole('navigation', { name: 'Account' })
    expect(within(nav).getByRole('link', { name: 'Tasks' })).toHaveAttribute('href', '/tasks')
  })

  describe('finding tasks', () => {
    it('sends the search, range and sort of the address to every column', async () => {
      const fetchMock = stubBoard({ ToDo: tasksOf('ToDo', 1) })
      renderApp('/tasks?search=demo&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc')

      await waitFor(() => expect(columnCalls(fetchMock)).toHaveLength(3))
      for (const params of columnCalls(fetchMock)) {
        expect(params.get('search')).toBe('demo')
        expect(params.get('dueFrom')).toBe('2026-12-01')
        expect(params.get('dueTo')).toBe('2026-12-31')
        expect(params.get('sort')).toBe('title')
        expect(params.get('direction')).toBe('desc')
      }
      expect(screen.getByLabelText('Search')).toHaveValue('demo')
    })

    it('ignores invalid and former list values in the address', async () => {
      const fetchMock = stubBoard({ ToDo: tasksOf('ToDo', 1) })
      renderApp('/tasks?status=Done&page=3&sort=priority')

      await waitFor(() => expect(columnCalls(fetchMock)).toHaveLength(3))
      expect(columnCalls(fetchMock).map((params) => [...params.keys()].sort().join(','))).toEqual([
        'pageSize,status',
        'pageSize,status',
        'pageSize,status',
      ])
    })

    it('shows the configured maximum range from the settings', async () => {
      stubBoard({ ToDo: tasksOf('ToDo', 1) })
      renderApp('/tasks')

      await waitFor(() => expect(screen.getByLabelText('Due to')).toHaveAccessibleDescription('Up to 90 days.'))
    })

    it('updates the address after the person stops typing a search', async () => {
      vi.useRealTimers()
      stubBoard({ ToDo: tasksOf('ToDo', 1) })
      const { router } = renderApp('/tasks?sort=title')
      const user = userEvent.setup()

      await user.type(await screen.findByLabelText('Search'), 'demo')

      expect(router.state.location.search).toBe('?sort=title')
      await waitFor(() => expect(router.state.location.search).toBe('?search=demo&sort=title'), { timeout: 2000 })
    })
  })
})
