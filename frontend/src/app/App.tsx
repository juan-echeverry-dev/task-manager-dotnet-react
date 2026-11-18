import { createBrowserRouter } from 'react-router'
import { AppProviders } from './AppProviders'
import { routes } from './routes'

const router = createBrowserRouter(routes)

/** Composition root of the SPA. */
export function App() {
  return (
    <AppProviders
      router={router}
      queryDefaults={{ queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false } }}
    />
  )
}
