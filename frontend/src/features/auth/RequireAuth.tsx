import { useEffect } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { isExpired } from './session/session-storage'
import { useSession } from './session/useSession'
import type { SignInLocationState } from './SignInPage'

/**
 * Layout route that only renders its children for signed-in people (spec FR-010, FR-012). Anyone
 * else goes to sign-in, remembering where they wanted to go, with the expiry message if the
 * session ran out (US3-3).
 */
export function RequireAuth() {
  const { session, endReason, endRefusedSession } = useSession()
  const location = useLocation()
  const expired = session !== null && isExpired(session)

  useEffect(() => {
    if (expired) endRefusedSession()
  }, [expired, endRefusedSession])

  if (session && !expired) {
    return <Outlet />
  }

  if (endReason === 'signed-out') {
    return <Navigate to="/sign-in" replace />
  }

  const returnTo = encodeURIComponent(`${location.pathname}${location.search}`)
  const state: SignInLocationState = { reason: expired || endReason === 'expired' ? 'expired' : undefined }
  return <Navigate to={`/sign-in?returnTo=${returnTo}`} state={state} replace />
}
