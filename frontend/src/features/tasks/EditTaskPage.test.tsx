import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { jsonResponse, problemResponse, stubFetch, type FetchHandler } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { aTask } from './test-data'

const task = aTask({ id: 'abc', title: 'Prepare demo', description: 'Slides', status: 'InProgress', dueDate: '2026-12-01' })

/** Answers GET with the task and PUT with `onPut`. */
function stubTaskApi(onPut: FetchHandler = (_url, init) => jsonResponse({ ...task, ...JSON.parse(String(init?.body)) })) {
  return stubFetch((url, init) => {
    if (url !== '/api/tasks/abc') return new Response(null, { status: 404 })
    return init?.method === 'PUT' ? onPut(url, init) : jsonResponse(task)
  })
}

async function openForm() {
  signedIn()
  const result = renderApp('/tasks/abc/edit')
  expect(await screen.findByRole('heading', { name: 'Edit task' })).toBeInTheDocument()
  await waitFor(() => expect(screen.getByLabelText('Title')).toHaveValue('Prepare demo'))
  return { ...result, user: userEvent.setup() }
}

function putBody(fetchMock: ReturnType<typeof stubFetch>) {
  const init = fetchMock.mock.calls.find(([, requestInit]) => requestInit?.method === 'PUT')?.[1]
  return JSON.parse(String(init?.body)) as unknown
}

describe('EditTaskPage', () => {
  afterEach(() => sessionStorage.clear())

  it('fills the form with the task values', async () => {
    stubTaskApi()
    await openForm()

    expect(screen.getByLabelText('Description')).toHaveValue('Slides')
    expect(screen.getByLabelText('Status')).toHaveValue('InProgress')
    expect(screen.getByLabelText('Due date')).toHaveValue('2026-12-01')
  })

  it('saves every value and opens the task with a confirmation', async () => {
    const fetchMock = stubTaskApi()
    const { user, router } = await openForm()
    await user.clear(screen.getByLabelText('Title'))
    await user.type(screen.getByLabelText('Title'), 'Prepare the demo')
    await user.selectOptions(screen.getByLabelText('Status'), 'Done')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks/abc'))
    expect(router.state.location.state).toEqual({ status: 'Task updated.' })
    expect(putBody(fetchMock)).toEqual({ title: 'Prepare the demo', description: 'Slides', status: 'Done', dueDate: '2026-12-01' })
  })

  it('can clear the description and the due date', async () => {
    const fetchMock = stubTaskApi()
    const { user } = await openForm()
    await user.clear(screen.getByLabelText('Description'))
    await user.clear(screen.getByLabelText('Due date'))

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(putBody(fetchMock)).toEqual({ title: 'Prepare demo', description: null, status: 'InProgress', dueDate: null }))
  })

  it('disables the submit button while saving', async () => {
    stubTaskApi(() => new Promise<Response>(() => {}) as unknown as Response)
    const { user } = await openForm()

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    const button = await screen.findByRole('button', { name: 'Saving…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('maps server validation errors to fields and keeps the values', async () => {
    stubTaskApi(() => problemResponse(400, { errors: { dueDate: ['Enter a due date between 2000-01-01 and 2099-12-31.'] } }))
    const { user } = await openForm()

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Enter a due date between 2000-01-01 and 2099-12-31.')).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('shows the service message on a network error and keeps the values', async () => {
    stubTaskApi(() => Promise.reject(new TypeError('Failed to fetch')) as unknown as Response)
    const { user } = await openForm()
    await user.type(screen.getByLabelText('Title'), '!')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(screen.getByLabelText('Title')).toHaveValue('Prepare demo!')
  })

  it('shows "Task not found" when the task does not exist', async () => {
    stubFetch(() => problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }))
    signedIn()
    renderApp('/tasks/abc/edit')

    expect(await screen.findByRole('heading', { name: 'Task not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
  })

  it('returns to the list with an alert when the task was deleted meanwhile', async () => {
    stubTaskApi(() => problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }))
    const { user, router } = await openForm()

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
    expect(router.state.location.state).toEqual({ alert: 'This task no longer exists.' })
  })
})
