import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { SessionProvider } from './SessionProvider'
import { SESSION_STORAGE_KEY } from './session-storage'
import { useSession } from './useSession'

function Probe() {
  const { session, signIn } = useSession()
  return (
    <div>
      <p>{session ? `token:${session.accessToken}` : 'anonymous'}</p>
      <button type="button" onClick={() => signIn({ accessToken: 'abc', expiresAt: '2026-11-18T10:00:00Z' })}>
        sign in
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
})
