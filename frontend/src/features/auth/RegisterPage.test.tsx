import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'

const profile = { id: '1', displayName: 'Ana', email: 'ana@example.com', createdAt: '2026-11-18T09:00:00+00:00' }

async function fillForm(values: { displayName?: string; email?: string; password?: string } = {}) {
  const user = userEvent.setup()
  const { displayName = 'Ana', email = 'ana@example.com', password = 'correct horse' } = values
  if (displayName) await user.type(screen.getByLabelText('Display name'), displayName)
  if (email) await user.type(screen.getByLabelText('Email'), email)
  if (password) await user.type(screen.getByLabelText('Password'), password)
  return user
}

function submitButton() {
  return screen.getByRole('button', { name: /create account|creating account/i })
}

describe('RegisterPage', () => {
  it('renders labelled fields', async () => {
    stubFetch()
    renderApp('/register')

    expect(await screen.findByRole('heading', { name: 'Create your account' })).toBeInTheDocument()
    expect(screen.getByLabelText('Display name')).toHaveAttribute('autocomplete', 'name')
    expect(screen.getByLabelText('Email')).toHaveAttribute('type', 'email')
    expect(screen.getByLabelText('Password')).toHaveAttribute('type', 'password')
  })

  it('shows client-side errors next to each invalid field without calling the API', async () => {
    const fetchMock = stubFetch()
    renderApp('/register')
    const user = await fillForm({ displayName: '   ', email: 'ana', password: 'short' })

    await user.click(submitButton())

    const displayName = screen.getByLabelText('Display name')
    expect(displayName).toHaveAttribute('aria-invalid', 'true')
    expect(displayName).toHaveAccessibleDescription('Display name is required.')
    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Enter a valid email address.')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription(
      'Password must be between 8 and 128 characters.',
    )
    expect(fetchMock.mock.calls.filter(([url]) => url !== '/health')).toHaveLength(0)
  })

  it('rejects a password made only of spaces', async () => {
    stubFetch()
    renderApp('/register')
    const user = await fillForm({ password: '          ' })

    await user.click(submitButton())

    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription('Password cannot be only spaces.')
  })

  it('disables the submit button while the request is pending', async () => {
    stubFetch(() => new Promise<Response>(() => {}))
    renderApp('/register')
    const user = await fillForm()

    await user.click(submitButton())

    const button = await screen.findByRole('button', { name: 'Creating account…' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('maps server validation errors to fields, keeps values and clears the password', async () => {
    stubFetch(() =>
      problemResponse(400, { title: 'Validation failed', errors: { email: ['Email must be at most 254 characters.'] } }),
    )
    renderApp('/register')
    const user = await fillForm()

    await user.click(submitButton())

    await waitFor(() =>
      expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Email must be at most 254 characters.'),
    )
    expect(screen.getByLabelText('Display name')).toHaveValue('Ana')
    expect(screen.getByLabelText('Email')).toHaveValue('ana@example.com')
    expect(screen.getByLabelText('Password')).toHaveValue('')
  })

  it('shows the duplicate-email message on the email field', async () => {
    stubFetch(() => problemResponse(409, { title: 'Email already registered' }))
    renderApp('/register')
    const user = await fillForm()

    await user.click(submitButton())

    await waitFor(() =>
      expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('This email is already registered.'),
    )
    expect(screen.getByLabelText('Password')).toHaveValue('')
  })

  it('shows a non-technical message when the service is unreachable and keeps the values', async () => {
    stubFetch(() => Promise.reject(new TypeError('Failed to fetch')))
    renderApp('/register')
    const user = await fillForm()

    await user.click(submitButton())

    expect(await screen.findByRole('alert')).toHaveTextContent("We couldn't reach the service. Please try again.")
    expect(screen.getByLabelText('Email')).toHaveValue('ana@example.com')
    expect(screen.getByLabelText('Password')).toHaveValue('')
  })

  it('can be completed with the keyboard alone', async () => {
    const fetchMock = stubFetch(() => jsonResponse(profile, 201))
    renderApp('/register')
    const user = userEvent.setup()
    await screen.findByLabelText('Display name')

    await user.tab()
    while (document.activeElement !== screen.getByLabelText('Display name')) await user.tab()
    await user.keyboard('Ana')
    await user.tab()
    expect(screen.getByLabelText('Email')).toHaveFocus()
    await user.keyboard('ana@example.com')
    await user.tab()
    expect(screen.getByLabelText('Password')).toHaveFocus()
    await user.keyboard('correct horse{Enter}')

    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => url === '/api/auth/register')).toBe(true))
  })

  it('goes to sign-in with the email pre-filled and a success message', async () => {
    stubFetch(() => jsonResponse(profile, 201))
    const { router } = renderApp('/register')
    const user = await fillForm({ email: ' ana@example.com ' })

    await user.click(submitButton())

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
    expect(await screen.findByRole('status')).toHaveTextContent('Account created. You can now sign in.')
    expect(screen.getByLabelText('Email')).toHaveValue('ana@example.com')
  })
})
