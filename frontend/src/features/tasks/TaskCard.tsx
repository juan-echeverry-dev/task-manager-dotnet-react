import { Link } from 'react-router'
import { OverdueBadge } from './badges'
import { boardHref } from './board-links'
import { formatDate } from './format'
import { isOverdue } from './overdue'
import type { Task } from './tasks-api'

interface TaskCardProps {
  task: Task
  /** Board query string, kept on the task's address. */
  search: string
  today: string
}

/** A task on the board: title, description excerpt, due date and overdue marker (spec 004 FR-003). */
export function TaskCard({ task, search, today }: TaskCardProps) {
  return (
    <li className="space-y-2 rounded-md border border-slate-200 bg-white p-3 shadow-sm">
      <Link
        to={boardHref(`/tasks/${task.id}`, search)}
        className="block break-words font-medium text-slate-900 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
      >
        {task.title}
      </Link>
      {task.description ? <p className="line-clamp-2 break-words text-sm text-slate-600">{task.description}</p> : null}
      <div className="flex flex-wrap items-center gap-2 text-xs">
        <span className="text-slate-500">{task.dueDate ? `Due ${formatDate(task.dueDate)}` : 'No due date'}</span>
        {isOverdue(task, today) ? <OverdueBadge /> : null}
      </div>
    </li>
  )
}
