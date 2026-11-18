import { vi } from 'vitest'

export type FetchHandler = (url: string, init: RequestInit | undefined) => Response | Promise<Response>

/** JSON response helper for fetch stubs. */
export function jsonResponse(body: unknown, status = 200, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

/** Problem Details response helper (RFC 9457). */
export function problemResponse(status: number, problem: Record<string, unknown>): Response {
  return jsonResponse({ status, ...problem }, status, 'application/problem+json')
}

/**
 * Stubs global fetch. The health check used by the layout always answers "Healthy"; every other
 * request goes to `handler`.
 */
export function stubFetch(handler: FetchHandler = () => new Response(null, { status: 404 })) {
  const fetchMock = vi.fn<typeof fetch>((input, init) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    if (url === '/health') {
      return Promise.resolve(new Response('Healthy'))
    }
    return Promise.resolve(handler(url, init))
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}
