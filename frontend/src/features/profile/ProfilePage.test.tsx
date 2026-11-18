import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { SESSION_STORAGE_KEY } from '../auth/session/session-storage'
import { jsonResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'

const profile = { id: '1', displayName: 'Ana', email: 'ana@example.com', createdAt: '2026-11-18T09:00:00+00:00' }

function signedIn(accessToken = 'token-123') {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken, expiresAt: '2099-01-01T00:00:00Z' }))
}

describe('ProfilePage', () => {
  afterEach(() => sessionStorage.clear())

  it('shows display name, email and member-since date using the session token', async () => {
    signedIn()
    const fetchMock = stubFetch((url) => (url === '/api/users/me' ? jsonResponse(profile) : new Response(null, { status: 404 })))
    renderApp('/profile')

    expect(await screen.findByRole('heading', { name: 'Your profile' })).toBeInTheDocument()
    expect(await screen.findByText('Ana')).toBeInTheDocument()
    expect(screen.getByText('ana@example.com')).toBeInTheDocument()
    const memberSince = new Intl.DateTimeFormat(undefined, { dateStyle: 'long' }).format(new Date(profile.createdAt))
    expect(screen.getByText(memberSince)).toBeInTheDocument()

    const init = fetchMock.mock.calls.find(([url]) => url === '/api/users/me')?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
  })
})
