import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { Pagination } from '../../shared/ui/Pagination'
import { useSession } from '../auth/session/useSession'
import { formatDate, STATUS_LABELS } from './format'
import { taskMessages } from './messages'
import { isOverdue, localToday } from './overdue'
import { listTasks, type Task } from './tasks-api'

/** Active task list (spec US1, FR-016 to FR-019). Rendered only inside RequireAuth. */
export function TaskListPage() {
  const accessToken = useSession().session?.accessToken ?? ''
  const [searchParams] = useSearchParams()
  const page = parsePage(searchParams.get('page'))
  const params = { page: page === 1 ? undefined : page }

  const tasks = useQuery({
    queryKey: ['tasks', 'list', params, accessToken],
    queryFn: () => listTasks(params, accessToken),
    enabled: accessToken !== '',
  })
  const today = localToday()

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">Tasks</h1>
        <Link
          to="/tasks/new"
          className="rounded-md bg-sky-700 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          New task
        </Link>
      </div>

      {tasks.isPending ? <p className="text-slate-600">Loading tasks…</p> : null}

      {tasks.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {tasks.data && tasks.data.totalCount === 0 ? (
        <div className="rounded-md border border-dashed border-slate-300 bg-white px-4 py-8 text-center">
          <p className="text-slate-700">You have no tasks yet.</p>
          <Link to="/tasks/new" className="mt-2 inline-block font-medium text-sky-700 underline">
            Create your first task
          </Link>
        </div>
      ) : null}

      {tasks.data && tasks.data.totalCount > 0 ? (
        <>
          <p className="text-sm text-slate-600">
            {tasks.data.totalCount} {tasks.data.totalCount === 1 ? 'task' : 'tasks'}
          </p>
          <ul aria-label="Tasks" className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
            {tasks.data.items.map((task) => (
              <TaskRow key={task.id} task={task} today={today} />
            ))}
          </ul>
          <Pagination
            page={tasks.data.page}
            pageSize={tasks.data.pageSize}
            totalCount={tasks.data.totalCount}
            hrefFor={(number) => (number === 1 ? '/tasks' : `/tasks?page=${number}`)}
          />
        </>
      ) : null}
    </section>
  )
}

function TaskRow({ task, today }: { task: Task; today: string }) {
  return (
    <li className="flex flex-col gap-2 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <Link
        to={`/tasks/${task.id}`}
        className="break-words font-medium text-slate-900 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
      >
        {task.title}
      </Link>
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <StatusBadge status={task.status} />
        <span className="text-slate-600">{task.dueDate ? `Due ${formatDate(task.dueDate)}` : 'No due date'}</span>
        {isOverdue(task, today) ? <OverdueBadge /> : null}
      </div>
    </li>
  )
}

export function StatusBadge({ status }: { status: Task['status'] }) {
  const tone =
    status === 'Done'
      ? 'bg-emerald-100 text-emerald-800'
      : status === 'InProgress'
        ? 'bg-amber-100 text-amber-900'
        : 'bg-slate-100 text-slate-800'
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${tone}`}>{STATUS_LABELS[status]}</span>
}

export function OverdueBadge() {
  return <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800">Overdue</span>
}

function parsePage(value: string | null): number {
  const page = Number(value)
  return Number.isInteger(page) && page >= 1 ? page : 1
}
