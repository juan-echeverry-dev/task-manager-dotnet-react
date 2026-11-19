import { useQuery } from '@tanstack/react-query'
import { Link, Outlet, useMatch, useSearchParams } from 'react-router'
import { useSession } from '../auth/session/useSession'
import { boardHref, boardSearch } from './board-links'
import { hasFilters, parseBoardQuery, serializeBoardQuery, withChanges, type BoardQuery } from './board-query'
import { BoardColumn } from './BoardColumn'
import { localToday } from './overdue'
import { StatusMessage } from './StatusMessage'
import { TaskFilters } from './TaskFilters'
import { getTaskSettings } from './tasks-api'
import { useBoardColumn } from './useBoardColumn'

/**
 * Task board (spec 004): three status columns under a filter bar. Search, range and sort live in the
 * address; the side panel (details, create, edit) renders in the outlet. Rendered only inside RequireAuth.
 */
export function TaskBoardPage() {
  const accessToken = useSession().session?.accessToken ?? ''
  const [searchParams, setSearchParams] = useSearchParams()
  const query = parseBoardQuery(searchParams)
  const search = boardSearch(query)
  const today = localToday()
  // The panel shows its own messages; the board shows them only when no panel is open.
  const onBoard = useMatch('/tasks') !== null

  const columns = [
    { status: 'ToDo', data: useBoardColumn('ToDo', query) },
    { status: 'InProgress', data: useBoardColumn('InProgress', query) },
    { status: 'Done', data: useBoardColumn('Done', query) },
  ] as const

  const settings = useQuery({
    queryKey: ['tasks', 'settings', accessToken],
    queryFn: () => getTaskSettings(accessToken),
    enabled: accessToken !== '',
    staleTime: Infinity,
  })

  const change = (changes: Partial<BoardQuery>) => setSearchParams(serializeBoardQuery(withChanges(query, changes)))
  const clear = () => change({ search: '', dueFrom: '', dueTo: '' })

  // The board-level empty states need all three counts (spec 004 FR-015).
  const counts = columns.map((column) => column.data.totalCount)
  const empty = counts.every((count) => count === 0)
  const filtered = hasFilters(query)

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 tabIndex={-1} className="text-2xl font-semibold focus:outline-none">
          Tasks
        </h1>
        <Link
          to={boardHref('/tasks/new', search)}
          className="rounded-md bg-sky-700 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-sky-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          New task
        </Link>
      </div>

      {onBoard ? <StatusMessage /> : null}

      <TaskFilters query={query} maxRangeDays={settings.data?.dueDateRangeMaxDays} onChange={change} />

      {empty ? (
        <div className="rounded-md border border-dashed border-slate-300 bg-white px-4 py-8 text-center">
          {filtered ? (
            <>
              <p className="text-slate-700">No tasks match your search.</p>
              <button type="button" onClick={clear} className="mt-2 font-medium text-sky-700 underline">
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
      ) : (
        <div className="grid items-start gap-4 lg:grid-cols-3">
          {columns.map((column) => (
            <BoardColumn key={column.status} status={column.status} column={column.data} search={search} today={today} />
          ))}
        </div>
      )}

      <Outlet />
    </section>
  )
}
