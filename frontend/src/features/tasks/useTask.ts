import { useQuery } from '@tanstack/react-query'
import { ApiError } from '../../shared/api/http-client'
import { useSession } from '../auth/session/useSession'
import { getTask } from './tasks-api'

export const taskQueryKey = (id: string) => ['tasks', 'detail', id] as const

/** Loads one task of the signed-in person. `notFound` is true for a missing or foreign task. */
export function useTask(id: string) {
  const accessToken = useSession().session?.accessToken ?? ''
  const query = useQuery({
    queryKey: [...taskQueryKey(id), accessToken],
    queryFn: () => getTask(id, accessToken),
    enabled: accessToken !== '',
  })
  const notFound = query.error instanceof ApiError && query.error.status === 404

  return { ...query, notFound, accessToken }
}
