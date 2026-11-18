import { useQuery } from '@tanstack/react-query'
import { request } from '../../shared/api/http-client'

const healthQuery = {
  queryKey: ['system', 'health'],
  queryFn: () => request<string>('/health'),
  refetchInterval: 30_000,
} as const

/** Small badge reporting whether the backend API is reachable and healthy. */
export function ApiStatus() {
  const { isPending, isSuccess } = useQuery(healthQuery)

  const [label, dotClass] = isPending
    ? ['Checking API…', 'bg-slate-400']
    : isSuccess
      ? ['API online', 'bg-emerald-500']
      : ['API unavailable', 'bg-red-500']

  return (
    <p role="status" className="inline-flex items-center gap-2 text-sm text-slate-600">
      <span aria-hidden="true" className={`size-2 rounded-full ${dotClass}`} />
      {label}
    </p>
  )
}
