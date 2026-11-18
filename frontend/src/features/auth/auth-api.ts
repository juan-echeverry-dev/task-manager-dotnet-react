import { request } from '../../shared/api/http-client'

/** Public profile of an account (contracts/identity-api.md). */
export interface UserProfile {
  id: string
  displayName: string
  email: string
  createdAt: string
}

export interface RegisterInput {
  displayName: string
  email: string
  password: string
}

export interface SignInInput {
  email: string
  password: string
}

export interface SignInResult {
  accessToken: string
  tokenType: 'Bearer'
  expiresAt: string
}

export function register(input: RegisterInput): Promise<UserProfile> {
  return postJson<UserProfile>('/api/auth/register', input)
}

export function signIn(input: SignInInput): Promise<SignInResult> {
  return postJson<SignInResult>('/api/auth/sign-in', input)
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
