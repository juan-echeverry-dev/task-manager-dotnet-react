import { useState } from 'react'
import { Link } from 'react-router'
import { useSession } from '../auth/session/useSession'
import { OverdueBadge } from './badges'
import { boardHref } from './board-links'
import { DeleteTaskButton } from './DeleteTaskButton'
import { formatDate } from './format'
import { taskMessages } from './messages'
import { isOverdue } from './overdue'
import type { Task } from './tasks-api'

interface TaskCardProps {
  task: Task
  /** Board query string, kept on the task's address. */
  search: string
  today: string
}

/**
 * A task on the board: title, description excerpt, due date and overdue marker (spec 004 FR-003), and a
 * "Delete" action that does not open the panel (FR-004, FR-013).
 */
export function TaskCard({ task, search, today }: TaskCardProps) {
  const accessToken = useSession().session?.accessToken ?? ''
  const [deleteFailed, setDeleteFailed] = useState(false)

  return (
    <li className="space-y-2 rounded-md border border-slate-200 bg-white p-3 shadow-sm">
      <Link
        to={boardHref(`/tasks/${task.id}`, search)}
        className="block break-words font-medium text-slate-900 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
      >
        {task.title}
      </Link>
      {task.description ? <p className="line-clamp-2 break-words text-sm text-slate-600">{task.description}</p> : null}
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-slate-500">{task.dueDate ? `Due ${formatDate(task.dueDate)}` : 'No due date'}</span>
          {isOverdue(task, today) ? <OverdueBadge /> : null}
        </div>
        <DeleteTaskButton
          task={task}
          accessToken={accessToken}
          label={`Delete ${task.title}`}
          onFailed={() => setDeleteFailed(true)}
          className="rounded px-1.5 py-0.5 font-medium text-slate-500 hover:bg-red-50 hover:text-red-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        />
      </div>
      {deleteFailed ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-2 py-1 text-xs text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}
    </li>
  )
}
