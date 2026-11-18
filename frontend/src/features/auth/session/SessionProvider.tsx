import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { clearSession, isExpired, readSession, writeSession, type Session } from './session-storage'
import { SessionContext, type SessionContextValue, type SessionEndReason } from './useSession'

interface SessionState {
  session: Session | null
  endReason: SessionEndReason | null
}

/**
 * Holds the session of this browser tab, backed by sessionStorage. It has no navigation logic, so it
 * can wrap the router; guards inside the router decide where to go.
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SessionState>(() => ({ session: readSession(), endReason: null }))

  const signIn = useCallback((session: Session) => {
    writeSession(session)
    setState({ session, endReason: null })
  }, [])

  const signOut = useCallback(() => {
    clearSession()
    setState({ session: null, endReason: 'signed-out' })
  }, [])

  const endRefusedSession = useCallback(() => {
    clearSession()
    setState((previous) =>
      previous.session
        ? { session: null, endReason: isExpired(previous.session) ? 'expired' : 'refused' }
        : previous,
    )
  }, [])

  const acknowledgeEnd = useCallback(() => {
    setState((previous) => (previous.endReason ? { ...previous, endReason: null } : previous))
  }, [])

  const value = useMemo<SessionContextValue>(
    () => ({ ...state, signIn, signOut, endRefusedSession, acknowledgeEnd }),
    [state, signIn, signOut, endRefusedSession, acknowledgeEnd],
  )

  return <SessionContext value={value}>{children}</SessionContext>
}
