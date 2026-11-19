import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { problemResponse } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { requestsWith, stubTasksApi } from './fake-tasks-api'
import { aTask } from './test-data'

const isPost = (init: RequestInit | undefined) => init?.method === 'POST'

async function openForm(path = '/tasks/new') {
  signedIn()
  const result = renderApp(path)
  const panel = await screen.findByRole('dialog', { name: 'New task' })
  return { ...result, panel, user: userEvent.setup() }
}

function postBody(fetchMock: ReturnType<typeof stubTasksApi>['fetchMock']) {
  return JSON.parse(String(requestsWith(fetchMock, 'POST')[0]?.[1]?.body)) as unknown
}

describe('NewTaskPanel', () => {
  afterEach(() => sessionStorage.clear())

  it('opens the create form in the panel with labelled fields and To do as the default status', async () => {
    stubTasksApi()
    const { panel } = await openForm()

    expect(within(panel).getByLabelText('Title')).toBeInTheDocument()
    expect(within(panel).getByLabelText('Description').tagName).toBe('TEXTAREA')
    expect(within(panel).getByLabelText('Status')).toHaveValue('ToDo')
    expect(within(panel).getAllByRole('option').map((option) => option.textContent)).toEqual(['To do', 'In progress', 'Done'])
    expect(within(panel).getByLabelText('Due date')).toHaveAttribute('type', 'date')
  })

  it.each([
    ['InProgress', 'InProgress'],
    ['Done', 'Done'],
    ['Later', 'ToDo'],
  ])('presets the status from ?status=%s', async (preset, expected) => {
    stubTasksApi()
    const { panel } = await openForm(`/tasks/new?status=${preset}`)

    expect(within(panel).getByLabelText('Status')).toHaveValue(expected)
  })

  it('creates the task, shows it with a confirmation, and adds its card to its column', async () => {
    const { fetchMock } = stubTasksApi([aTask({ id: 'old', title: 'Older task', status: 'InProgress' })])
    const { panel, user, router } = await openForm('/tasks/new?status=InProgress&search=task')
    await waitFor(() => expect(within(screen.getByRole('region', { name: 'In progress' })).getByText('1 task')).toBeInTheDocument())
    await user.type(within(panel).getByLabelText('Title'), '  Prepare demo task ')
    await user.type(within(panel).getByLabelText('Description'), 'Slides')
    await user.type(within(panel).getByLabelText('Due date'), '2026-12-01')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks/new-1'))
    expect(router.state.location.search).toBe('?search=task')
    expect(router.state.location.state).toEqual({ status: 'Task created.' })
    expect(postBody(fetchMock)).toEqual({
      title: 'Prepare demo task',
      description: 'Slides',
      status: 'InProgress',
      dueDate: '2026-12-01',
    })
    const details = await screen.findByRole('dialog', { name: 'Prepare demo task' })
    expect(within(details).getByText('Task created.')).toBeInTheDocument()
    expect(await within(screen.getByRole('list', { name: 'In progress' })).findByText('Prepare demo task')).toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'In progress' })).getByText('2 tasks')).toBeInTheDocument()
  })

  it('saves a task that does not match the current search, without adding a card', async () => {
    stubTasksApi([aTask({ id: 'old', title: 'Older task', status: 'ToDo' })])
    const { panel, user } = await openForm('/tasks/new?search=older')
    await waitFor(() => expect(within(screen.getByRole('region', { name: 'To do' })).getByText('1 task')).toBeInTheDocument())
    await user.type(within(panel).getByLabelText('Title'), 'Unrelated')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    expect(await screen.findByRole('dialog', { name: 'Unrelated' })).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'To do' })).queryByText('Unrelated')).not.toBeInTheDocument()
    expect(within(screen.getByRole('region', { name: 'To do' })).getByText('1 task')).toBeInTheDocument()
  })

  it('sends empty optional fields as null', async () => {
    const { fetchMock } = stubTasksApi()
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    await waitFor(() =>
      expect(postBody(fetchMock)).toEqual({ title: 'Prepare demo', description: null, status: 'ToDo', dueDate: null }),
    )
  })

  it('shows client-side errors on each invalid field without calling the API', async () => {
    const { fetchMock } = stubTasksApi()
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), '   ')
    await user.click(within(panel).getByLabelText('Description'))
    await user.paste('d'.repeat(2001))

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    expect(within(panel).getByLabelText('Title')).toHaveAccessibleDescription('Title is required.')
    expect(within(panel).getByLabelText('Description')).toHaveAccessibleDescription(
      'Description must be at most 2,000 characters.',
    )
    expect(requestsWith(fetchMock, 'POST')).toHaveLength(0)
  })

  it('rejects a due date outside the allowed range', async () => {
    stubTasksApi()
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')
    await user.type(within(panel).getByLabelText('Due date'), '2100-01-01')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    expect(within(panel).getByLabelText('Due date')).toHaveAccessibleDescription(
      'Enter a due date between 2000-01-01 and 2099-12-31.',
    )
  })

  it('disables the submit button while the request is pending', async () => {
    stubTasksApi([], (_url, init) => (isPost(init) ? (new Promise<Response>(() => {}) as unknown as Response) : undefined))
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    const button = await within(panel).findByRole('button', { name: 'Creating…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('maps server validation errors to fields and keeps the values', async () => {
    stubTasksApi([], (_url, init) =>
      isPost(init) ? problemResponse(400, { errors: { title: ['Title must be at most 200 characters.'] } }) : undefined,
    )
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    expect(await within(panel).findByText('Title must be at most 200 characters.')).toBeInTheDocument()
    expect(within(panel).getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('shows the service message on a network error and keeps the values', async () => {
    stubTasksApi([], (_url, init) =>
      isPost(init) ? (Promise.reject(new TypeError('Failed to fetch')) as unknown as Response) : undefined,
    )
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    expect(await within(panel).findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(within(panel).getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('ends the session when the API refuses it', async () => {
    stubTasksApi([], (_url, init) => (isPost(init) ? problemResponse(401, { title: 'Authentication required' }) : undefined))
    const { panel, user, router } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Create task' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
  })

  it('creates nothing when the panel is closed', async () => {
    const { fetchMock } = stubTasksApi()
    const { panel, user, router } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), 'Prepare demo')

    await user.click(within(panel).getByRole('button', { name: 'Close' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
    expect(requestsWith(fetchMock, 'POST')).toHaveLength(0)
  })
})
