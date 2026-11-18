import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { routes } from '../app/routes'
import { SessionProvider } from '../features/auth/session/SessionProvider'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
}

/** Renders a component with a fresh, non-retrying QueryClient so tests stay isolated and fast. */
export function renderWithProviders(ui: ReactElement) {
  return render(<QueryClientProvider client={createTestQueryClient()}>{ui}</QueryClientProvider>)
}

/**
 * Renders the real route table at `path` with the same providers as the app. The session is read
 * from sessionStorage, so tests sign in by seeding it. The returned router exposes the location.
 */
export function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  const result = render(
    <SessionProvider>
      <QueryClientProvider client={createTestQueryClient()}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </SessionProvider>,
  )
  return { ...result, router }
}
