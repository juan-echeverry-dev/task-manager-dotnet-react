import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { SESSION_STORAGE_KEY } from './session/session-storage'

const EXPIRED_MESSAGE = 'Your session has expired. Please sign in again.'
const profile = { id: '1', displayName: 'Ana', email: 'ana@example.com', createdAt: '2026-11-18T09:00:00+00:00' }

function storeSession(expiresAt: string) {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken: 'token-123', expiresAt }))
}

describe('session lifecycle', () => {
  afterEach(() => {
    sessionStorage.clear()
    vi.useRealTimers()
  })

  it('keeps a signed-in person on the profile after a reload', async () => {
    storeSession('2099-01-01T00:00:00Z')
    stubFetch(() => jsonResponse(profile))

    const { router } = renderApp('/profile')

    expect(await screen.findByText('Ana')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/profile')
  })

  it('sends a person whose stored session has expired to sign-in with the expiry message', async () => {
    storeSession('2000-01-01T00:00:00Z')
    stubFetch(() => jsonResponse(profile))

    const { router } = renderApp('/profile')

    expect(await screen.findByRole('alert')).toHaveTextContent(EXPIRED_MESSAGE)
    expect(router.state.location.pathname).toBe('/sign-in')
    expect(router.state.location.search).toBe('?returnTo=%2Fprofile')
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('ends the session with the expiry message when the API refuses it after expiry', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-11-18T09:59:59Z'))
    storeSession('2026-11-18T10:00:00Z')
    stubFetch(() => {
      // The session expires while the request is in flight.
      vi.setSystemTime(new Date('2026-11-18T10:00:01Z'))
      return problemResponse(401, { title: 'Unauthorized' })
    })

    const { router } = renderApp('/profile')

    expect(await screen.findByRole('alert')).toHaveTextContent(EXPIRED_MESSAGE)
    expect(router.state.location.pathname).toBe('/sign-in')
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it('treats a refused, unexpired session like no session, without the expiry message', async () => {
    storeSession('2099-01-01T00:00:00Z')
    stubFetch(() => problemResponse(401, { title: 'Unauthorized' }))

    const { router } = renderApp('/profile')

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
    expect(router.state.location.search).toBe('?returnTo=%2Fprofile')
    expect(screen.queryByText(EXPIRED_MESSAGE)).not.toBeInTheDocument()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })

  it.each(['/sign-in', '/register'])('redirects a signed-in person away from %s to the profile', async (path) => {
    storeSession('2099-01-01T00:00:00Z')
    stubFetch(() => jsonResponse(profile))

    const { router } = renderApp(path)

    await waitFor(() => expect(router.state.location.pathname).toBe('/profile'))
  })

  it('signs out from the profile and then requires signing in again', async () => {
    storeSession('2099-01-01T00:00:00Z')
    stubFetch(() => jsonResponse(profile))
    const { router } = renderApp('/profile')
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Sign out' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
    expect(router.state.location.search).toBe('')
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()

    await router.navigate('/profile')
    await waitFor(() => expect(router.state.location.search).toBe('?returnTo=%2Fprofile'))
    expect(screen.queryByText('Ana')).not.toBeInTheDocument()
  })

  it('shows session-aware navigation links', async () => {
    stubFetch(() => jsonResponse(profile))
    renderApp('/')

    expect(await screen.findByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/sign-in')
    expect(screen.getByRole('link', { name: 'Create account' })).toHaveAttribute('href', '/register')
  })

  it('shows a profile link when signed in', async () => {
    storeSession('2099-01-01T00:00:00Z')
    stubFetch(() => jsonResponse(profile))
    renderApp('/')

    expect(await screen.findByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/profile')
    expect(screen.queryByRole('link', { name: 'Sign in' })).not.toBeInTheDocument()
  })
})
