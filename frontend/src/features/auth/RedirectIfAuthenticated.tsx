import { Navigate, Outlet, useSearchParams } from 'react-router'
import { safeReturnPath } from './return-path'
import { isExpired } from './session/session-storage'
import { useSession } from './session/useSession'

/**
 * Layout route for the sign-in and registration screens: signed-in people are sent on to the safe
 * return destination, or their profile (US3-4). This is also how a successful sign-in navigates.
 */
export function RedirectIfAuthenticated() {
  const { session } = useSession()
  const [searchParams] = useSearchParams()

  if (session && !isExpired(session)) {
    return <Navigate to={safeReturnPath(searchParams.get('returnTo'))} replace />
  }

  return <Outlet />
}
