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

export function register(input: RegisterInput): Promise<UserProfile> {
  return request<UserProfile>('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
}
