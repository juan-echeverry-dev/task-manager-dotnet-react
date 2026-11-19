import { useId } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { STATUS_LABELS } from './format'
import { taskMessages } from './messages'
import { STATUS_STYLES } from './status-styles'
import { TaskCard } from './TaskCard'
import type { TaskStatus } from './tasks-api'
import type { BoardColumnData } from './useBoardColumn'

interface BoardColumnProps {
  status: TaskStatus
  column: BoardColumnData
  /** Board query string, kept on every link. */
  search: string
  today: string
}

/** One status column of the board: header with count, cards, "Load more" (spec 004 FR-001 to FR-006). */
export function BoardColumn({ status, column, search, today }: BoardColumnProps) {
  const headingId = useId()
  const label = STATUS_LABELS[status]
  const styles = STATUS_STYLES[status]

  return (
    <section
      aria-labelledby={headingId}
      className={`flex min-w-0 flex-col rounded-lg border border-t-4 border-slate-200 bg-slate-100/60 ${styles.column}`}
    >
      <header className={`flex items-center justify-between gap-2 rounded-t-md px-3 py-2 ${styles.header}`}>
        <h2 id={headingId} className="flex items-center gap-2 text-sm font-semibold text-slate-800">
          <span aria-hidden="true" className={`size-2.5 rounded-full ${styles.dot}`} />
          {label}
        </h2>
        <div className="flex items-center gap-2">
          {column.totalCount !== undefined ? (
            <span className="text-xs font-medium text-slate-600">{countLabel(column.totalCount)}</span>
          ) : null}
          <Link
            to={`/tasks/new?status=${status}${search ? `&${search.slice(1)}` : ''}`}
            aria-label={`New task in ${label}`}
            className="flex size-6 items-center justify-center rounded-md text-base leading-none font-semibold text-slate-600 hover:bg-white hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
          >
            <span aria-hidden="true">+</span>
          </Link>
        </div>
      </header>

      <div className="flex-1 space-y-3 p-3">
        {column.isLoading ? <p className="text-sm text-slate-600">Loading tasks…</p> : null}

        {column.error ? (
          <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
            {columnErrorMessage(column.error)}
          </p>
        ) : null}

        {!column.isLoading && !column.error && column.tasks.length === 0 ? (
          <p className="text-sm text-slate-500">No tasks</p>
        ) : null}

        <ul aria-label={label} className="space-y-2">
          {column.tasks.map((task) => (
            <TaskCard key={task.id} task={task} search={search} today={today} />
          ))}
        </ul>

        {column.hasMore ? (
          <button
            type="button"
            onClick={column.loadMore}
            disabled={column.isLoadingMore}
            aria-busy={column.isLoadingMore}
            className="w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {column.isLoadingMore ? 'Loading more…' : 'Load more'}
          </button>
        ) : null}
      </div>
    </section>
  )
}

function countLabel(count: number): string {
  return `${count} ${count === 1 ? 'task' : 'tasks'}`
}

/** A refused query explains itself (for example a range longer than allowed); anything else is a service error. */
function columnErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 400) {
    const first = Object.values(error.problem?.errors ?? {})[0]?.[0]
    if (first) return first
  }
  return taskMessages.serviceUnavailable
}
