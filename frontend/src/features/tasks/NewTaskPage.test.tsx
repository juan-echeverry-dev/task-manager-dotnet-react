import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { aTask } from './test-data'

function apiCalls(fetchMock: ReturnType<typeof stubFetch>) {
  return fetchMock.mock.calls.filter(([url]) => url !== '/health')
}

async function openForm() {
  signedIn()
  const result = renderApp('/tasks/new')
  expect(await screen.findByRole('heading', { name: 'New task' })).toBeInTheDocument()
  return { ...result, user: userEvent.setup() }
}

describe('NewTaskPage', () => {
  afterEach(() => sessionStorage.clear())

  it('renders labelled fields with To do as the default status', async () => {
    stubFetch()
    await openForm()

    expect(screen.getByLabelText('Title')).toBeInTheDocument()
    expect(screen.getByLabelText('Description').tagName).toBe('TEXTAREA')
    const status = screen.getByLabelText('Status')
    expect(status).toHaveValue('ToDo')
    expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual(['To do', 'In progress', 'Done'])
    expect(screen.getByLabelText('Due date')).toHaveAttribute('type', 'date')
  })

  it('shows client-side errors on each invalid field without calling the API', async () => {
    const fetchMock = stubFetch()
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), '   ')
    await user.click(screen.getByLabelText('Description'))
    await user.paste('d'.repeat(2001))

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(screen.getByLabelText('Title')).toHaveAccessibleDescription('Title is required.')
    expect(screen.getByLabelText('Description')).toHaveAccessibleDescription(
      'Description must be at most 2,000 characters.',
    )
    expect(apiCalls(fetchMock)).toHaveLength(0)
  })

  it('rejects a due date outside the allowed range', async () => {
    stubFetch()
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')
    await user.type(screen.getByLabelText('Due date'), '2100-01-01')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(screen.getByLabelText('Due date')).toHaveAccessibleDescription(
      'Enter a due date between 2000-01-01 and 2099-12-31.',
    )
  })

  it('creates the task and opens it with a confirmation', async () => {
    const created = aTask({ id: 'abc', title: 'Prepare demo', status: 'InProgress', dueDate: '2026-12-01' })
    const fetchMock = stubFetch((url, init) =>
      url === '/api/tasks' && init?.method === 'POST' ? jsonResponse(created, 201) : jsonResponse(created),
    )
    const { user, router } = await openForm()
    await user.type(screen.getByLabelText('Title'), '  Prepare demo ')
    await user.type(screen.getByLabelText('Description'), 'Slides')
    await user.selectOptions(screen.getByLabelText('Status'), 'InProgress')
    await user.type(screen.getByLabelText('Due date'), '2026-12-01')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks/abc'))
    expect(router.state.location.state).toEqual({ status: 'Task created.' })
    const [, init] = apiCalls(fetchMock).find(([, requestInit]) => requestInit?.method === 'POST') ?? []
    expect(JSON.parse(String(init?.body))).toEqual({
      title: 'Prepare demo',
      description: 'Slides',
      status: 'InProgress',
      dueDate: '2026-12-01',
    })
  })

  it('sends empty optional fields as null', async () => {
    const fetchMock = stubFetch(() => jsonResponse(aTask(), 201))
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    await waitFor(() => expect(apiCalls(fetchMock).length).toBeGreaterThan(0))
    const [, init] = apiCalls(fetchMock)[0] ?? []
    expect(JSON.parse(String(init?.body))).toEqual({ title: 'Prepare demo', description: null, status: 'ToDo', dueDate: null })
  })

  it('disables the submit button while the request is pending', async () => {
    stubFetch(() => new Promise<Response>(() => {}))
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    const button = await screen.findByRole('button', { name: 'Creating…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('maps server validation errors to fields and keeps the values', async () => {
    stubFetch(() => problemResponse(400, { errors: { title: ['Title must be at most 200 characters.'] } }))
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByText('Title must be at most 200 characters.')).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('shows the service message on a network error and keeps the values', async () => {
    stubFetch(() => Promise.reject(new TypeError('Failed to fetch')))
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(screen.getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('ends the session when the API refuses it', async () => {
    stubFetch(() => problemResponse(401, { title: 'Authentication required' }))
    const { user, router } = await openForm()
    await user.type(screen.getByLabelText('Title'), 'Prepare demo')

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
  })
})
