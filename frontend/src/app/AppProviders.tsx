import { QueryCache, QueryClient, QueryClientProvider, type DefaultOptions } from '@tanstack/react-query'
import { useEffect, useState, type ReactNode } from 'react'
import { RouterProvider, type createBrowserRouter } from 'react-router'
import { SessionProvider } from '../features/auth/session/SessionProvider'
import { useSession } from '../features/auth/session/useSession'
import { ApiError } from '../shared/api/http-client'

type AppRouter = ReturnType<typeof createBrowserRouter>

interface AppProvidersProps {
  router: AppRouter
  queryDefaults?: DefaultOptions
}

/** Global providers shared by the app and the tests: session, server state, and routing. */
export function AppProviders({ router, queryDefaults }: AppProvidersProps) {
  return (
    <SessionProvider>
      <SessionAwareQueryClient defaults={queryDefaults}>
        <RouterProvider router={router} />
      </SessionAwareQueryClient>
    </SessionProvider>
  )
}

/**
 * Creates the query client. Any 401 from a query ends the session; the route guard then sends the
 * person to sign-in. Cached server data is dropped whenever the session ends.
 */
function SessionAwareQueryClient({ defaults, children }: { defaults: DefaultOptions | undefined; children: ReactNode }) {
  const { session, endRefusedSession } = useSession()
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: defaults,
        queryCache: new QueryCache({
          onError: (error) => {
            if (error instanceof ApiError && error.status === 401) endRefusedSession()
          },
        }),
      }),
  )

  useEffect(() => {
    if (!session) queryClient.removeQueries({ queryKey: ['profile'] })
  }, [session, queryClient])

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}
