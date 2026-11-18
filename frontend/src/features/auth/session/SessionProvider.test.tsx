import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { SessionProvider } from './SessionProvider'
import { SESSION_STORAGE_KEY } from './session-storage'
import { useSession } from './useSession'

function Probe() {
  const { session, signIn, signOut } = useSession()
  return (
    <div>
      <p>{session ? `token:${session.accessToken}` : 'anonymous'}</p>
      <button type="button" onClick={() => signIn({ accessToken: 'abc', expiresAt: '2026-11-18T10:00:00Z' })}>
        sign in
      </button>
      <button type="button" onClick={signOut}>
        sign out
      </button>
    </div>
  )
}

describe('SessionProvider', () => {
  afterEach(() => sessionStorage.clear())

  it('stores the session in sessionStorage when signing in', () => {
    render(
      <SessionProvider>
        <Probe />
      </SessionProvider>,
    )

    act(() => screen.getByRole('button', { name: 'sign in' }).click())

    expect(screen.getByText('token:abc')).toBeInTheDocument()
    expect(JSON.parse(sessionStorage.getItem(SESSION_STORAGE_KEY) ?? 'null')).toEqual({
      accessToken: 'abc',
      expiresAt: '2026-11-18T10:00:00Z',
    })
  })

  it.each(['not json', '{"accessToken":1}', '{"expiresAt":"2026-11-18T10:00:00Z"}'])(
    'discards an invalid stored value: %s',
    (stored) => {
      sessionStorage.setItem(SESSION_STORAGE_KEY, stored)

      render(
        <SessionProvider>
          <Probe />
        </SessionProvider>,
      )

      expect(screen.getByText('anonymous')).toBeInTheDocument()
      expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
    },
  )

  it('restores the session of this tab after a reload', () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' }))

    render(
      <SessionProvider>
        <Probe />
      </SessionProvider>,
    )

    expect(screen.getByText('token:abc')).toBeInTheDocument()
  })

  it('clears the stored session when signing out', () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ accessToken: 'abc', expiresAt: '2099-01-01T00:00:00Z' }))
    render(
      <SessionProvider>
        <Probe />
      </SessionProvider>,
    )

    act(() => screen.getByRole('button', { name: 'sign out' }).click())

    expect(screen.getByText('anonymous')).toBeInTheDocument()
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull()
  })
})
