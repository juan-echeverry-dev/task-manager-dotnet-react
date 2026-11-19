import { SESSION_STORAGE_KEY } from '../features/auth/session/session-storage'

/** Seeds a valid session for the current tab, as the sign-in screen would. */
export function signedIn(accessToken = 'token-123') {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken, expiresAt: '2099-01-01T00:00:00Z' }))
}
