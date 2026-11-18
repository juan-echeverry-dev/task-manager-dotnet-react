import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'
import { SESSION_STORAGE_KEY } from './session/session-storage'

const tokenResponse = { accessToken: 'token-123', tokenType: 'Bearer', expiresAt: '2099-01-01T00:00:00Z' }
const profile = { id: '1', displayName: 'Ana', email: 'ana@example.com', createdAt: '2026-11-18T09:00:00+00:00' }

function api(signIn: () => Response | Promise<Response>) {
  return stubFetch((url) => (url === '/api/auth/sign-in' ? signIn() : jsonResponse(profile)))
}

async function signInWith(email = 'ana@example.com', password = 'correct horse') {
  const user = userEvent.setup()
  await user.type(await screen.findByLabelText('Email'), email)
  await user.type(screen.getByLabelText('Password'), password)
  await user.click(screen.getByRole('button', { name: /sign in|signing in/i }))
  return user
}

describe('SignInPage', () => {
  afterEach(() => sessionStorage.clear())

  it('renders labelled fields', async () => {
    stubFetch()
    renderApp('/sign-in')

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveAttribute('autocomplete', 'email')
    expect(screen.getByLabelText('Password')).toHaveAttribute('autocomplete', 'current-password')
  })

  it('requires both fields before calling the API', async () => {
    const fetchMock = stubFetch()
    renderApp('/sign-in')
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Sign in' }))

    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Email is required.')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription('Password is required.')
    expect(fetchMock.mock.calls.filter(([url]) => url !== '/health')).toHaveLength(0)
  })

  it('disables the submit button while signing in', async () => {
    api(() => new Promise<Response>(() => {}))
    renderApp('/sign-in')

    await signInWith()

    const button = await screen.findByRole('button', { name: 'Signing in…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('shows the uniform failure message and clears the password', async () => {
    api(() => problemResponse(401, { title: 'Invalid credentials', detail: 'Invalid email or password.' }))
    renderApp('/sign-in')

    await signInWith()

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.')
    expect(screen.getByLabelText('Email')).toHaveValue('ana@example.com')
    expect(screen.getByLabelText('Password')).toHaveValue('')
  })

  it('shows a non-technical message when the service is unreachable', async () => {
    api(() => Promise.reject(new TypeError('Failed to fetch')))
    renderApp('/sign-in')

    await signInWith()

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(screen.getByLabelText('Email')).toHaveValue('ana@example.com')
    expect(screen.getByLabelText('Password')).toHaveValue('')
  })

  it('submits with the Enter key', async () => {
    const fetchMock = api(() => jsonResponse(tokenResponse))
    renderApp('/sign-in')
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Email'), 'ana@example.com')
    await user.type(screen.getByLabelText('Password'), 'correct horse{Enter}')

    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => url === '/api/auth/sign-in')).toBe(true))
  })

  it('stores the session and goes to the profile by default', async () => {
    api(() => jsonResponse(tokenResponse))
    const { router } = renderApp('/sign-in')

    await signInWith()

    await waitFor(() => expect(router.state.location.pathname).toBe('/profile'))
    expect(JSON.parse(sessionStorage.getItem(SESSION_STORAGE_KEY) ?? 'null')).toEqual({
      accessToken: 'token-123',
      expiresAt: '2099-01-01T00:00:00Z',
    })
  })

  it('returns to a safe internal destination after signing in', async () => {
    api(() => jsonResponse(tokenResponse))
    const { router } = renderApp('/sign-in?returnTo=%2F')

    await signInWith()

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
  })

  it('ignores an external return destination', async () => {
    api(() => jsonResponse(tokenResponse))
    const { router } = renderApp(`/sign-in?returnTo=${encodeURIComponent('https://evil.example')}`)

    await signInWith()

    await waitFor(() => expect(router.state.location.pathname).toBe('/profile'))
  })
})
