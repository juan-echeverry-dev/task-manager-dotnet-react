import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { SessionProvider } from '../features/auth/session/SessionProvider'
import { routes } from './routes'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false },
  },
})

const router = createBrowserRouter(routes)

/** Composition root of the SPA: global providers and the router. */
export function App() {
  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </SessionProvider>
  )
}
