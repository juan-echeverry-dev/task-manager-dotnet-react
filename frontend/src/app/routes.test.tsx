import { screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../test/render'
import { routes } from './routes'

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  return renderWithProviders(<RouterProvider router={router} />)
}

describe('routes', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(new Response('Healthy')))
  })

  it('renders the home page at the root path', async () => {
    renderAt('/')

    expect(await screen.findByRole('heading', { name: 'Welcome to TaskLane' })).toBeInTheDocument()
  })

  it('renders the not-found page for unknown paths', async () => {
    renderAt('/does-not-exist')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to the home page' })).toHaveAttribute('href', '/')
  })
})
