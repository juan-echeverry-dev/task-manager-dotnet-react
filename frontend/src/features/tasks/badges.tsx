import { STATUS_LABELS } from './format'
import type { TaskStatus } from './tasks-api'

const STATUS_TONES: Record<TaskStatus, string> = {
  ToDo: 'bg-slate-100 text-slate-800',
  InProgress: 'bg-amber-100 text-amber-900',
  Done: 'bg-emerald-100 text-emerald-800',
}

export function StatusBadge({ status }: { status: TaskStatus }) {
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_TONES[status]}`}>{STATUS_LABELS[status]}</span>
  )
}

export function OverdueBadge() {
  return <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800">Overdue</span>
}
