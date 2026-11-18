/** Client-side session: the access token and when it expires (contracts/spa-routes.md). */
export interface Session {
  accessToken: string
  expiresAt: string
}

export const SESSION_STORAGE_KEY = 'tasklane.session'

/**
 * Reads the session of this browser tab. sessionStorage survives reloads and is cleared when the
 * tab closes (spec FR-009). Malformed values are discarded.
 */
export function readSession(): Session | null {
  const raw = sessionStorage.getItem(SESSION_STORAGE_KEY)
  if (raw === null) {
    return null
  }

  try {
    const value: unknown = JSON.parse(raw)
    if (isSession(value)) {
      return { accessToken: value.accessToken, expiresAt: value.expiresAt }
    }
  } catch {
    // Fall through: invalid JSON is treated like no session.
  }

  clearSession()
  return null
}

export function writeSession(session: Session): void {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

export function clearSession(): void {
  sessionStorage.removeItem(SESSION_STORAGE_KEY)
}

export function isExpired(session: Session, now: number = Date.now()): boolean {
  return Date.parse(session.expiresAt) <= now
}

function isSession(value: unknown): value is Session {
  if (typeof value !== 'object' || value === null) {
    return false
  }
  const candidate = value as Record<string, unknown>
  return (
    typeof candidate.accessToken === 'string' &&
    candidate.accessToken.length > 0 &&
    typeof candidate.expiresAt === 'string' &&
    !Number.isNaN(Date.parse(candidate.expiresAt))
  )
}
