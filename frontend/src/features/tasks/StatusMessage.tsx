import { useLocation } from 'react-router'
import type { TaskStatusState } from './messages'

/** One-time confirmation passed by the previous screen in the navigation state ("Task created."). */
export function StatusMessage() {
  const status = (useLocation().state as TaskStatusState | null)?.status
  if (!status) return null

  return (
    <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900">
      {status}
    </p>
  )
}
