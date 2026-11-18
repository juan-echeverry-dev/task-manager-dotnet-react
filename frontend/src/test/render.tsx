import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { createMemoryRouter } from 'react-router'
import { AppProviders } from '../app/AppProviders'
import { routes } from '../app/routes'

/** Renders a component with a fresh, non-retrying QueryClient so tests stay isolated and fast. */
export function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

/**
 * Renders the real route table at `path` with the same providers as the app. The session is read
 * from sessionStorage, so tests sign in by seeding it. The returned router exposes the location.
 */
export function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  const result = render(
    <AppProviders router={router} queryDefaults={{ queries: { retry: false }, mutations: { retry: false } }} />,
  )
  return { ...result, router }
}
