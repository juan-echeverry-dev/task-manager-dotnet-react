import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { renderApp } from '../../test/render'
import { signedIn } from '../../test/session'
import { stubTasksApi } from './fake-tasks-api'
import { aTask } from './test-data'

const task = aTask({ id: 'abc', title: 'Prepare demo', description: 'Slides', status: 'ToDo' })

describe('TaskPanel', () => {
  beforeEach(() => signedIn())
  afterEach(() => sessionStorage.clear())

  it('opens over the board, which stays rendered with its query string', async () => {
    stubTasksApi([task])
    renderApp('/tasks/abc?search=demo')

    const panel = await screen.findByRole('dialog', { name: 'Prepare demo' })
    expect(panel).toHaveAttribute('open')
    expect(await screen.findByRole('list', { name: 'To do' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Tasks' })).toBeInTheDocument()
  })

  it('closes with the close button, replacing the address with the board and its query string', async () => {
    stubTasksApi([task])
    const { router } = renderApp('/tasks/abc?search=demo')
    const user = userEvent.setup()

    await user.click(within(await screen.findByRole('dialog', { name: 'Prepare demo' })).getByRole('button', { name: 'Close' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
    expect(router.state.location.search).toBe('?search=demo')
    expect(router.state.historyAction).toBe('REPLACE')
    expect(screen.queryByRole('dialog', { name: 'Prepare demo' })).not.toBeInTheDocument()
  })

  it('closes with Escape', async () => {
    stubTasksApi([task])
    const { router } = renderApp('/tasks/abc')
    const user = userEvent.setup()
    await screen.findByRole('dialog', { name: 'Prepare demo' })

    await user.keyboard('{Escape}')

    await waitFor(() => expect(router.state.location.pathname).toBe('/tasks'))
  })

  it('opens from a card, and closing it returns focus to the card without leaving a way back into the panel', async () => {
    stubTasksApi([task])
    const { router } = renderApp('/tasks?search=demo')
    const user = userEvent.setup()

    const card = await within(await screen.findByRole('list', { name: 'To do' })).findByRole('link', { name: 'Prepare demo' })
    await user.click(card)
    const panel = await screen.findByRole('dialog', { name: 'Prepare demo' })
    expect(router.state.location.pathname).toBe('/tasks/abc')

    await user.click(within(panel).getByRole('button', { name: 'Close' }))

    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Prepare demo' })).not.toBeInTheDocument())
    expect(router.state.location.pathname).toBe('/tasks')
    await waitFor(() => expect(card).toHaveFocus())

    await router.navigate(-1)
    expect(router.state.location.pathname).toBe('/tasks')
  })

  it('closes when the browser goes back', async () => {
    stubTasksApi([task])
    const { router } = renderApp('/tasks')
    const user = userEvent.setup()
    await user.click(await within(await screen.findByRole('list', { name: 'To do' })).findByRole('link', { name: 'Prepare demo' }))
    await screen.findByRole('dialog', { name: 'Prepare demo' })

    await router.navigate(-1)

    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Prepare demo' })).not.toBeInTheDocument())
  })

  it('opens a task hidden by the current search', async () => {
    stubTasksApi([task])
    renderApp('/tasks/abc?search=zzz')

    expect(await screen.findByRole('dialog', { name: 'Prepare demo' })).toBeInTheDocument()
    expect(await screen.findByText('No tasks match your search.')).toBeInTheDocument()
  })

  it('shows "Task not found" in the panel for a missing task', async () => {
    stubTasksApi([task])
    renderApp('/tasks/missing')

    const panel = await screen.findByRole('dialog', { name: 'Task not found' })
    expect(within(panel).getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
  })
})
