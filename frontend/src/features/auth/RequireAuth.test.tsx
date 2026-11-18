import { screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { stubFetch } from '../../test/fetch'
import { renderApp } from '../../test/render'

describe('RequireAuth', () => {
  afterEach(() => sessionStorage.clear())

  it('sends anonymous visitors to sign-in, remembering where they wanted to go', async () => {
    stubFetch()
    const { router } = renderApp('/profile')

    await waitFor(() => expect(router.state.location.pathname).toBe('/sign-in'))
    expect(router.state.location.search).toBe('?returnTo=%2Fprofile')
    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })
})
