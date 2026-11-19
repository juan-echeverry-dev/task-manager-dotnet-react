import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { Pagination } from '../../shared/ui/Pagination'
import { useSession } from '../auth/session/useSession'
import { OverdueBadge, StatusBadge } from './badges'
import { formatDate } from './format'
import { taskMessages } from './messages'
import { isOverdue, localToday } from './overdue'
import { StatusMessage } from './StatusMessage'
import { TaskFilters } from './TaskFilters'
import {
  hasFilters,
  parseTaskQuery,
  serializeTaskQuery,
  toApiParams,
  withChanges,
  type TaskQuery,
} from './task-query'
import { getTaskSettings, listTasks, type Task } from './tasks-api'

const LIST_PATH = '/tasks'
const LIST_TITLE = 'Tasks'

/**
 * Task list (spec US1, US4). Search, filters, sort and page live in the address, so reloading or
 * sharing it shows the same list. Rendered only inside RequireAuth.
 */
export function TaskListPage() {
  const accessToken = useSession().session?.accessToken ?? ''
  const [searchParams, setSearchParams] = useSearchParams()
  const query = parseTaskQuery(searchParams)
  const params = toApiParams(query)

  const tasks = useQuery({
    queryKey: ['tasks', 'list', params, accessToken],
    queryFn: () => listTasks(params, accessToken),
    enabled: accessToken !== '',
  })
  const settings = useQuery({
    queryKey: ['tasks', 'settings', accessToken],
    queryFn: () => getTaskSettings(accessToken),
    enabled: accessToken !== '',
    staleTime: Infinity,
  })

  const change = (changes: Partial<TaskQuery>) => setSearchParams(serializeTaskQuery(withChanges(query, changes)))
  const hrefFor = (page: number) => {
    const search = serializeTaskQuery({ ...query, page }).toString()
    return search ? `${LIST_PATH}?${search}` : LIST_PATH
  }
  const filtered = hasFilters(query)
  const today = localToday()

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">{LIST_TITLE}</h1>
        <Link
          to="/tasks/new"
          className="rounded-md bg-sky-700 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          New task
        </Link>
      </div>

      <StatusMessage />

      <TaskFilters query={query} maxRangeDays={settings.data?.dueDateRangeMaxDays} onChange={change} />

      {tasks.isPending ? <p className="text-slate-600">Loading tasks…</p> : null}

      {tasks.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {listErrorMessage(tasks.error)}
        </p>
      ) : null}

      {tasks.data && tasks.data.totalCount === 0 ? (
        <div className="rounded-md border border-dashed border-slate-300 bg-white px-4 py-8 text-center">
          {filtered ? (
            <>
              <p className="text-slate-700">No tasks match your search.</p>
              <button
                type="button"
                onClick={() => change({ search: '', status: '', dueFrom: '', dueTo: '' })}
                className="mt-2 font-medium text-sky-700 underline"
              >
                Clear search and filters
              </button>
            </>
          ) : (
            <>
              <p className="text-slate-700">You have no tasks yet.</p>
              <Link to="/tasks/new" className="mt-2 inline-block font-medium text-sky-700 underline">
                Create your first task
              </Link>
            </>
          )}
        </div>
      ) : null}

      {tasks.data && tasks.data.totalCount > 0 ? (
        <>
          <p className="text-sm text-slate-600">
            {tasks.data.totalCount} {tasks.data.totalCount === 1 ? 'task' : 'tasks'}
          </p>
          {tasks.data.items.length > 0 ? (
            <ul aria-label={LIST_TITLE} className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
              {tasks.data.items.map((task) => (
                <TaskRow key={task.id} task={task} today={today} />
              ))}
            </ul>
          ) : (
            <p className="text-slate-700">This page is empty.</p>
          )}
          <Pagination
            page={tasks.data.page}
            pageSize={tasks.data.pageSize}
            totalCount={tasks.data.totalCount}
            hrefFor={hrefFor}
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

/** A refused query explains itself (for example a range longer than allowed); anything else is a service error. */
function listErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 400) {
    const first = Object.values(error.problem?.errors ?? {})[0]?.[0]
    if (first) return first
  }
  return taskMessages.serviceUnavailable
}
