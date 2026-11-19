import { useEffect, useEffectEvent, useState } from 'react'
import { SelectField } from '../../shared/ui/SelectField'
import { TextField } from '../../shared/ui/TextField'
import { STATUS_LABELS } from './format'
import {
  daysBetween,
  hasFilters,
  isDate,
  SORT_LABELS,
  sortFields,
  type TaskListMode,
  type TaskQuery,
} from './task-query'
import { TASK_STATUSES } from './tasks-api'

const SEARCH_DELAY_MS = 300

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  ...TASK_STATUSES.map((status) => ({ value: status, label: STATUS_LABELS[status] })),
]
const DIRECTION_OPTIONS = [
  { value: 'asc', label: 'Ascending' },
  { value: 'desc', label: 'Descending' },
]

const buttonClass =
  'rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'

interface TaskFiltersProps {
  query: TaskQuery
  mode: TaskListMode
  /** Longest due-date range allowed by the server; unknown until the settings load. */
  maxRangeDays: number | undefined
  onChange: (changes: Partial<TaskQuery>) => void
}

interface RangeErrors {
  dueFrom?: string
  dueTo?: string
}

/** Search, status, due-date range and sort controls of the task lists (spec US4). */
export function TaskFilters({ query, mode, maxRangeDays, onChange }: TaskFiltersProps) {
  const [search, setSearch] = useState(query.search)
  const [dueFrom, setDueFrom] = useState(query.dueFrom)
  const [dueTo, setDueTo] = useState(query.dueTo)
  const [rangeErrors, setRangeErrors] = useState<RangeErrors>({})

  // Follow the address when it changes from outside (back button, links, "clear"), during render as
  // React recommends for state derived from props.
  const [shown, setShown] = useState({ search: query.search, dueFrom: query.dueFrom, dueTo: query.dueTo })
  if (shown.search !== query.search || shown.dueFrom !== query.dueFrom || shown.dueTo !== query.dueTo) {
    if (shown.search !== query.search) setSearch(query.search)
    if (shown.dueFrom !== query.dueFrom || shown.dueTo !== query.dueTo) {
      setDueFrom(query.dueFrom)
      setDueTo(query.dueTo)
    }
    setShown({ search: query.search, dueFrom: query.dueFrom, dueTo: query.dueTo })
  }

  // Apply the search once the person stops typing.
  const applySearch = useEffectEvent((value: string) => onChange({ search: value }))
  useEffect(() => {
    const trimmed = search.trim()
    if (trimmed === query.search) return
    const timer = setTimeout(() => applySearch(trimmed), SEARCH_DELAY_MS)
    return () => clearTimeout(timer)
  }, [search, query.search])

  const applyRange = () => {
    const errors = validateRange(dueFrom, dueTo, maxRangeDays)
    setRangeErrors(errors)
    if (errors.dueFrom || errors.dueTo) return
    onChange({ dueFrom, dueTo })
  }

  const clear = () => {
    setSearch('')
    setDueFrom('')
    setDueTo('')
    setRangeErrors({})
    onChange({ search: '', status: '', dueFrom: '', dueTo: '' })
  }

  const rangeHint = maxRangeDays === undefined ? undefined : `Up to ${maxRangeDays} days.`

  return (
    <div role="search" aria-label="Find tasks" className="space-y-4 rounded-md border border-slate-200 bg-white p-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField
          label="Search"
          type="search"
          value={search}
          maxLength={200}
          onChange={(event) => setSearch(event.target.value)}
        />
        <SelectField
          label="Status"
          options={STATUS_OPTIONS}
          value={query.status}
          onChange={(event) => onChange({ status: event.target.value as TaskQuery['status'] })}
        />
      </div>

      <form
        noValidate
        className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-start"
        onSubmit={(event) => {
          event.preventDefault()
          applyRange()
        }}
      >
        <TextField
          label="Due from"
          type="date"
          value={dueFrom}
          error={rangeErrors.dueFrom}
          onChange={(event) => setDueFrom(event.target.value)}
        />
        <TextField
          label="Due to"
          type="date"
          value={dueTo}
          hint={rangeHint}
          error={rangeErrors.dueTo}
          onChange={(event) => setDueTo(event.target.value)}
        />
        <button type="submit" className={`${buttonClass} sm:mt-6`}>
          Apply
        </button>
      </form>

      <div className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
        <SelectField
          label="Sort by"
          options={sortFields(mode).map((field) => ({ value: field, label: SORT_LABELS[field] }))}
          value={query.sort}
          onChange={(event) => onChange({ sort: event.target.value as TaskQuery['sort'] })}
        />
        <SelectField
          label="Order"
          options={DIRECTION_OPTIONS}
          value={query.direction}
          onChange={(event) => onChange({ direction: event.target.value as TaskQuery['direction'] })}
        />
        {hasFilters(query) ? (
          <button type="button" className={buttonClass} onClick={clear}>
            Clear search and filters
          </button>
        ) : null}
      </div>
    </div>
  )
}

/** The same rules and messages as the API (data-model.md). */
function validateRange(dueFrom: string, dueTo: string, maxRangeDays: number | undefined): RangeErrors {
  if (dueFrom === '' && dueTo === '') return {}
  if (dueFrom === '') return { dueFrom: 'Choose both a start and an end date.' }
  if (dueTo === '') return { dueTo: 'Choose both a start and an end date.' }
  if (!isDate(dueFrom)) return { dueFrom: 'Enter dates as YYYY-MM-DD.' }
  if (!isDate(dueTo)) return { dueTo: 'Enter dates as YYYY-MM-DD.' }
  if (dueFrom > dueTo) return { dueTo: 'The start date must be on or before the end date.' }
  if (maxRangeDays !== undefined && daysBetween(dueFrom, dueTo) > maxRangeDays) {
    return { dueTo: `Choose a range of at most ${maxRangeDays} days.` }
  }
  return {}
}
