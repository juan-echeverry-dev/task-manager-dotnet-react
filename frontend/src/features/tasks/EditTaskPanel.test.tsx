import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { problemResponse } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { requestsWith, stubTasksApi } from './fake-tasks-api'
import { aTask } from './test-data'

const task = aTask({ id: 'abc', title: 'Prepare demo', description: 'Slides', status: 'InProgress', dueDate: '2026-12-01' })

async function openForm(path = '/tasks/abc/edit') {
  signedIn()
  const result = renderApp(path)
  const panel = await screen.findByRole('dialog', { name: 'Edit task' })
  await waitFor(() => expect(within(panel).getByLabelText('Title')).toHaveValue('Prepare demo'))
  return { ...result, panel, user: userEvent.setup() }
}

function putBody(fetchMock: ReturnType<typeof stubTasksApi>['fetchMock']) {
  return JSON.parse(String(requestsWith(fetchMock, 'PUT')[0]?.[1]?.body)) as unknown
}

describe('EditTaskPanel', () => {
  afterEach(() => sessionStorage.clear())

  it('fills the form in the panel with the task values', async () => {
    stubTasksApi([task])
    const { panel } = await openForm()

    expect(within(panel).getByLabelText('Description')).toHaveValue('Slides')
    expect(within(panel).getByLabelText('Status')).toHaveValue('InProgress')
    expect(within(panel).getByLabelText('Due date')).toHaveValue('2026-12-01')
  })

  it('saves, shows the task with a confirmation, and moves its card to the new status', async () => {
    const { fetchMock } = stubTasksApi([task])
    const { panel, user, router } = await openForm('/tasks/abc/edit?search=demo')
    expect(await within(await screen.findByRole('list', { name: 'In progress' })).findByText('Prepare demo')).toBeInTheDocument()
    await user.clear(within(panel).getByLabelText('Title'))
    await user.type(within(panel).getByLabelText('Title'), 'Prepare the demo')
    await user.selectOptions(within(panel).getByLabelText('Status'), 'Done')

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks/abc'))
    expect(router.state.location.search).toBe('?search=demo')
    expect(router.state.location.state).toEqual({ status: 'Task updated.' })
    expect(putBody(fetchMock)).toEqual({ title: 'Prepare the demo', description: 'Slides', status: 'Done', dueDate: '2026-12-01' })
    const details = await screen.findByRole('dialog', { name: 'Prepare the demo' })
    expect(within(details).getByText('Task updated.')).toBeInTheDocument()
    expect(await within(screen.getByRole('list', { name: 'Done' })).findByText('Prepare the demo')).toBeInTheDocument()
    expect(within(screen.getByRole('list', { name: 'In progress' })).queryByText('Prepare demo')).not.toBeInTheDocument()
  })

  it('returns to the details unchanged on Cancel', async () => {
    const { fetchMock } = stubTasksApi([task])
    const { panel, user, router } = await openForm('/tasks/abc/edit?search=demo')
    await user.type(within(panel).getByLabelText('Title'), '!')

    await user.click(within(panel).getByRole('link', { name: 'Cancel' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks/abc'))
    expect(router.state.location.search).toBe('?search=demo')
    expect(await screen.findByRole('dialog', { name: 'Prepare demo' })).toBeInTheDocument()
    expect(requestsWith(fetchMock, 'PUT')).toHaveLength(0)
  })

  it('can clear the description and the due date', async () => {
    const { fetchMock } = stubTasksApi([task])
    const { panel, user } = await openForm()
    await user.clear(within(panel).getByLabelText('Description'))
    await user.clear(within(panel).getByLabelText('Due date'))

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    await waitFor(() =>
      expect(putBody(fetchMock)).toEqual({ title: 'Prepare demo', description: null, status: 'InProgress', dueDate: null }),
    )
  })

  it('disables the submit button while saving', async () => {
    stubTasksApi([task], (_url, init) => (init?.method === 'PUT' ? (new Promise<Response>(() => {}) as unknown as Response) : undefined))
    const { panel, user } = await openForm()

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    const button = await within(panel).findByRole('button', { name: 'Saving…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('maps server validation errors to fields and keeps the values', async () => {
    stubTasksApi([task], (_url, init) =>
      init?.method === 'PUT'
        ? problemResponse(400, { errors: { dueDate: ['Enter a due date between 2000-01-01 and 2099-12-31.'] } })
        : undefined,
    )
    const { panel, user } = await openForm()

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    expect(await within(panel).findByText('Enter a due date between 2000-01-01 and 2099-12-31.')).toBeInTheDocument()
    expect(within(panel).getByLabelText('Title')).toHaveValue('Prepare demo')
  })

  it('shows the service message on a network error and keeps the values', async () => {
    stubTasksApi([task], (_url, init) =>
      init?.method === 'PUT' ? (Promise.reject(new TypeError('Failed to fetch')) as unknown as Response) : undefined,
    )
    const { panel, user } = await openForm()
    await user.type(within(panel).getByLabelText('Title'), '!')

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    expect(await within(panel).findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(within(panel).getByLabelText('Title')).toHaveValue('Prepare demo!')
  })

  it('shows "Task not found" in the panel when the task does not exist', async () => {
    stubTasksApi([])
    signedIn()
    renderApp('/tasks/abc/edit')

    const panel = await screen.findByRole('dialog', { name: 'Task not found' })
    expect(within(panel).getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
  })

  it('returns to the board with an alert when the task was deleted meanwhile', async () => {
    stubTasksApi([task], (_url, init) =>
      init?.method === 'PUT' ? problemResponse(404, { title: 'Task not found', detail: 'The task does not exist.' }) : undefined,
    )
    const { panel, user, router } = await openForm()

    await user.click(within(panel).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
    expect(router.state.location.state).toEqual({ alert: 'This task no longer exists.' })
  })
})
