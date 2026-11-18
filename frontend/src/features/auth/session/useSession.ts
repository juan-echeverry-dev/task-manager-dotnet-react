import { createContext, useContext } from 'react'
import type { Session } from './session-storage'

export interface SessionContextValue {
  session: Session | null
  signIn: (session: Session) => void
  signOut: () => void
}

export const SessionContext = createContext<SessionContextValue | null>(null)

/** Current session of this tab and the actions that change it. */
export function useSession(): SessionContextValue {
  const context = useContext(SessionContext)
  if (context === null) {
    throw new Error('useSession must be used inside <SessionProvider>.')
  }
  return context
}
