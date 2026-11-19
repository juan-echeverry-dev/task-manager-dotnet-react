import { useLocation } from 'react-router'
import type { TaskStatusState } from './messages'

/** One-time message passed by the previous screen in the navigation state. */
export function StatusMessage() {
  const state = useLocation().state as TaskStatusState | null

  return (
    <>
      {state?.status ? (
        <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900">
          {state.status}
        </p>
      ) : null}
      {state?.alert ? (
        <p role="alert" className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
          {state.alert}
        </p>
      ) : null}
    </>
  )
}
