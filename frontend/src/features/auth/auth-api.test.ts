import { describe, expect, it } from 'vitest'
import { ApiError } from '../../shared/api/http-client'
import { jsonResponse, problemResponse, stubFetch } from '../../test/fetch'
import { register } from './auth-api'

const input = { displayName: 'Ana', email: 'ana@example.com', password: 'correct horse' }

describe('register', () => {
  it('posts the account as JSON and returns the created profile', async () => {
    const profile = { id: '1', displayName: 'Ana', email: 'ana@example.com', createdAt: '2026-11-18T09:00:00+00:00' }
    const fetchMock = stubFetch(() => jsonResponse(profile, 201))

    await expect(register(input)).resolves.toEqual(profile)

    const [url, init] = fetchMock.mock.calls[0] ?? []
    expect(url).toBe('/api/auth/register')
    expect(init?.method).toBe('POST')
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
    expect(JSON.parse(String(init?.body))).toEqual(input)
  })

  it.each([400, 409])('surfaces a %i response as ApiError with the problem details', async (status) => {
    stubFetch(() => problemResponse(status, { title: 'Failed' }))

    const error = await register(input).catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(status)
    expect((error as ApiError).problem?.title).toBe('Failed')
  })
})
