import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { clearSession, readSession, writeSession, type Session } from './session-storage'
import { SessionContext, type SessionContextValue } from './useSession'

/**
 * Holds the session of this browser tab, backed by sessionStorage. It has no navigation logic, so it
 * can wrap the router; screens and guards inside the router decide where to go.
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(readSession)

  const signIn = useCallback((next: Session) => {
    writeSession(next)
    setSession(next)
  }, [])

  const signOut = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const value = useMemo<SessionContextValue>(() => ({ session, signIn, signOut }), [session, signIn, signOut])

  return <SessionContext value={value}>{children}</SessionContext>
}
