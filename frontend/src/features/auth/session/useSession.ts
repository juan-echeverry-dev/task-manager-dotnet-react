import { createContext, useContext } from 'react'
import type { Session } from './session-storage'

/**
 * Why the last session ended, so the sign-in screen can explain it: `expired` shows the expiry
 * message, `refused` (tampered, revoked, or orphaned) shows nothing, `signed-out` was intentional.
 */
export type SessionEndReason = 'expired' | 'refused' | 'signed-out'

export interface SessionContextValue {
  session: Session | null
  endReason: SessionEndReason | null
  signIn: (session: Session) => void
  signOut: () => void
  /** Ends the session after the API refused it, or when it is found expired. */
  endRefusedSession: () => void
  /** Called once the end reason has been shown, so it does not affect later navigation. */
  acknowledgeEnd: () => void
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
