import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { OverdueBadge, StatusBadge } from './badges'
import { boardHref } from './board-links'
import { DeleteTaskButton } from './DeleteTaskButton'
import { formatDate, formatTimestamp } from './format'
import { taskMessages } from './messages'
import { isOverdue, localToday } from './overdue'
import { StatusMessage } from './StatusMessage'
import { useTaskPanel } from './task-panel-context'
import { TaskNotFound } from './TaskNotFound'
import { useTask } from './useTask'

const actionClass =
  'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60'

/** Task details in the side panel, with Edit and Delete (spec 004 US2, FR-007). */
export function TaskDetailPanel() {
  const { id = '' } = useParams()
  const { titleId, search } = useTaskPanel()
  const task = useTask(id)
  const [actionError, setActionError] = useState<string>()

  if (task.notFound) return <TaskNotFound titleId={titleId} />

  return (
    <div className="space-y-6">
      {task.data ? (
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h2 id={titleId} className="min-w-0 break-words text-xl font-semibold">
            {task.data.title}
          </h2>
          <div className="flex gap-2">
            <Link to={boardHref(`/tasks/${task.data.id}/edit`, search)} className={actionClass}>
              Edit
            </Link>
            <DeleteTaskButton
              task={task.data}
              accessToken={task.accessToken}
              onFailed={() => setActionError(taskMessages.serviceUnavailable)}
              className={actionClass}
            />
          </div>
        </div>
      ) : (
        <h2 id={titleId} className="text-xl font-semibold">
          Task
        </h2>
      )}

      <StatusMessage />

      {task.isPending ? <p className="text-slate-600">Loading task…</p> : null}

      {task.isError || actionError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {task.data ? (
        <dl className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
          <DetailRow label="Status">
            <span className="flex flex-wrap items-center gap-2">
              <StatusBadge status={task.data.status} />
              {isOverdue(task.data, localToday()) ? <OverdueBadge /> : null}
            </span>
          </DetailRow>
          <DetailRow label="Due date">{task.data.dueDate ? formatDate(task.data.dueDate) : 'No due date'}</DetailRow>
          <DetailRow label="Description">
            {task.data.description ? (
              <span className="whitespace-pre-wrap">{task.data.description}</span>
            ) : (
              <span className="text-slate-500">No description</span>
            )}
          </DetailRow>
          <DetailRow label="Created">{formatTimestamp(task.data.createdAt)}</DetailRow>
          <DetailRow label="Last updated">{formatTimestamp(task.data.updatedAt)}</DetailRow>
        </dl>
      ) : null}
    </div>
  )
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1 px-4 py-3">
      <dt className="text-sm font-medium text-slate-500">{label}</dt>
      <dd className="min-w-0 break-words text-slate-900">{children}</dd>
    </div>
  )
}
