import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../../test/render'
import { ApiStatus } from './ApiStatus'

describe('ApiStatus', () => {
  it('reports the API as online when the health check succeeds', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(new Response('Healthy')))

    renderWithProviders(<ApiStatus />)

    expect(await screen.findByText('API online')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('API online')
  })

  it('shows a pending state while the health check is in flight', () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockReturnValue(new Promise<Response>(() => {})))

    renderWithProviders(<ApiStatus />)

    expect(screen.getByRole('status')).toHaveTextContent('Checking API…')
  })

  it('reports the API as unavailable when the health check fails', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(new Response('Unhealthy', { status: 503 })))

    renderWithProviders(<ApiStatus />)

    expect(await screen.findByText('API unavailable')).toBeInTheDocument()
  })
})
