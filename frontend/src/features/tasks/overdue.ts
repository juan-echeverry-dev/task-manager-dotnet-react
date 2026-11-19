import type { Task } from './tasks-api'

/**
 * An unfinished task whose due date is before the person's local today (spec FR-012). Dates are
 * `yyyy-MM-dd`, so comparing the strings compares the dates.
 */
export function isOverdue(task: Pick<Task, 'status' | 'dueDate'>, today: string): boolean {
  return task.status !== 'Done' && task.dueDate !== null && task.dueDate < today
}

/** Today's date in the person's time zone, as `yyyy-MM-dd`. */
export function localToday(now = new Date()): string {
  return toDateString(now)
}

function toDateString(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}
