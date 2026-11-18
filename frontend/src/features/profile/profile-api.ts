import { request } from '../../shared/api/http-client'
import type { UserProfile } from '../auth/auth-api'

export function getProfile(accessToken: string): Promise<UserProfile> {
  return request<UserProfile>('/api/users/me', {}, { accessToken })
}
