import type { TaskStatus } from './tasks-api'

/**
 * Board column accents, in the same palette as the status badges (spec 004 FR-002): neutral slate for
 * To do, amber for In progress, emerald for Done. Visual only; the column name is always shown.
 */
export const STATUS_STYLES: Record<TaskStatus, { column: string; dot: string; header: string }> = {
  ToDo: { column: 'border-t-slate-400', dot: 'bg-slate-400', header: 'bg-slate-50' },
  InProgress: { column: 'border-t-amber-500', dot: 'bg-amber-500', header: 'bg-amber-50' },
  Done: { column: 'border-t-emerald-500', dot: 'bg-emerald-500', header: 'bg-emerald-50' },
}
