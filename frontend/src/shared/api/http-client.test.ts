import { describe, expect, it, vi } from 'vitest'
import { ApiError, request } from './http-client'

function stubFetch(response: Response) {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('request', () => {
  it('returns the parsed JSON body of a successful response', async () => {
    stubFetch(Response.json({ id: 1 }))

    await expect(request<{ id: number }>('/api/things/1')).resolves.toEqual({ id: 1 })
  })

  it('returns the text body when the response is not JSON', async () => {
    stubFetch(new Response('Healthy', { headers: { 'content-type': 'text/plain' } }))

    await expect(request<string>('/health')).resolves.toBe('Healthy')
  })

  it('returns undefined for 204 No Content', async () => {
    stubFetch(new Response(null, { status: 204 }))

    await expect(request('/api/things/1', { method: 'DELETE' })).resolves.toBeUndefined()
  })

  it('sends an Accept header while keeping caller headers', async () => {
    const fetchMock = stubFetch(Response.json({}))

    await request('/api/things', { headers: { 'X-Custom': 'value' } })

    const headers = new Headers(fetchMock.mock.calls[0]?.[1]?.headers)
    expect(headers.get('Accept')).toBe('application/json')
    expect(headers.get('X-Custom')).toBe('value')
  })

  it('throws ApiError carrying the problem details of an error response', async () => {
    const problem = { title: 'Validation failed', status: 400, errors: { name: ['Required'] } }
    stubFetch(Response.json(problem, { status: 400, headers: { 'content-type': 'application/problem+json' } }))

    const error = await request('/api/things').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: 'Validation failed', problem })
  })

  it('throws ApiError with a generic message when the error body is not JSON', async () => {
    stubFetch(new Response('Service Unavailable', { status: 503 }))

    await expect(request('/health')).rejects.toMatchObject({
      status: 503,
      message: 'Request failed with status 503',
      problem: undefined,
    })
  })

  it('adds a bearer Authorization header when an access token is given', async () => {
    const fetchMock = stubFetch(Response.json({}))

    await request('/api/users/me', {}, { accessToken: 'token-123' })

    const init = fetchMock.mock.calls[0]?.[1]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer token-123')
  })

  it('sends no Authorization header without an access token', async () => {
    const fetchMock = stubFetch(Response.json({}))

    await request('/api/things')

    const init = fetchMock.mock.calls[0]?.[1]
    expect(new Headers(init?.headers).has('Authorization')).toBe(false)
  })
})
