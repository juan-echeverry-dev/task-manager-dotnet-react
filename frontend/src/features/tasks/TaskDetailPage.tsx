import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { OverdueBadge, StatusBadge } from './badges'
import { formatDate, formatTimestamp } from './format'
import { taskMessages } from './messages'
import { isOverdue, localToday } from './overdue'
import { StatusMessage } from './StatusMessage'
import { TaskNotFound } from './TaskNotFound'
import type { Task } from './tasks-api'
import { useTask } from './useTask'

const actionClass =
  'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60'

/** Task detail (spec US2). Rendered only inside RequireAuth. */
export function TaskDetailPage() {
  const { id = '' } = useParams()
  const task = useTask(id)

  if (task.notFound) return <TaskNotFound />

  return (
    <section className="mx-auto w-full max-w-2xl space-y-6">
      <Link to="/tasks" className="text-sm font-medium text-sky-700 underline-offset-4 hover:underline">
        ‹ Back to tasks
      </Link>
      <StatusMessage />

      {task.isPending ? <p className="text-slate-600">Loading task…</p> : null}

      {task.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {task.data ? <TaskDetails task={task.data} /> : null}
    </section>
  )
}

function TaskDetails({ task }: { task: Task }) {
  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <h1 className="break-words text-2xl font-semibold">{task.title}</h1>
        <div className="flex gap-2">
          <Link to={`/tasks/${task.id}/edit`} className={actionClass}>
            Edit
          </Link>
          <button type="button" className={actionClass}>
            Delete
          </button>
        </div>
      </div>

      <dl className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
        <DetailRow label="Status">
          <span className="flex flex-wrap items-center gap-2">
            <StatusBadge status={task.status} />
            {isOverdue(task, localToday()) ? <OverdueBadge /> : null}
          </span>
        </DetailRow>
        <DetailRow label="Due date">{task.dueDate ? formatDate(task.dueDate) : 'No due date'}</DetailRow>
        <DetailRow label="Description">
          {task.description ? (
            <span className="whitespace-pre-wrap">{task.description}</span>
          ) : (
            <span className="text-slate-500">No description</span>
          )}
        </DetailRow>
        <DetailRow label="Created">{formatTimestamp(task.createdAt)}</DetailRow>
        <DetailRow label="Last updated">{formatTimestamp(task.updatedAt)}</DetailRow>
      </dl>
    </>
  )
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1 px-4 py-3 sm:flex-row sm:gap-4">
      <dt className="text-sm font-medium text-slate-500 sm:w-36 sm:shrink-0">{label}</dt>
      <dd className="min-w-0 break-words text-slate-900">{children}</dd>
    </div>
  )
}
