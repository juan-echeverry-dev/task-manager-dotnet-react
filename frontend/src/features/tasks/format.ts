import type { TaskStatus } from './tasks-api'

export const STATUS_LABELS: Record<TaskStatus, string> = {
  ToDo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
}

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })
const timestampFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

/** Formats a `yyyy-MM-dd` calendar date in the local format, without shifting it across time zones. */
export function formatDate(date: string): string {
  const [year, month, day] = date.split('-').map(Number)
  return dateFormat.format(new Date(year ?? 0, (month ?? 1) - 1, day ?? 1))
}

/** Formats an ISO 8601 timestamp in local time. */
export function formatTimestamp(timestamp: string): string {
  return timestampFormat.format(new Date(timestamp))
}
