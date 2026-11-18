import { Navigate, Outlet, useLocation } from 'react-router'
import { useSession } from './session/useSession'

/** Layout route that only renders its children for signed-in people (spec FR-010, FR-012). */
export function RequireAuth() {
  const { session } = useSession()
  const location = useLocation()

  if (!session) {
    const returnTo = encodeURIComponent(`${location.pathname}${location.search}`)
    return <Navigate to={`/sign-in?returnTo=${returnTo}`} replace />
  }

  return <Outlet />
}
