import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { stubFetch } from '../test/fetch'
import { renderApp } from '../test/render'
import { signedIn } from '../test/session'

describe('routes', () => {
  beforeEach(() => {
    stubFetch()
  })

  it('renders the home page at the root path', async () => {
    renderApp('/')

    expect(await screen.findByRole('heading', { name: 'Welcome to TaskLane' })).toBeInTheDocument()
  })

  it('renders the not-found page for unknown paths', async () => {
    renderApp('/does-not-exist')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to the home page' })).toHaveAttribute('href', '/')
  })

  it('shows "Task not found" at the former deleted-tasks address', async () => {
    signedIn()
    renderApp('/tasks/deleted')

    expect(await screen.findByRole('heading', { name: 'Task not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to tasks' })).toHaveAttribute('href', '/tasks')
    sessionStorage.clear()
  })
})
