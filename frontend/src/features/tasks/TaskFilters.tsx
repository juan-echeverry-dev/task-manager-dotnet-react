import { useEffect, useEffectEvent, useState } from 'react'
import { SelectField } from '../../shared/ui/SelectField'
import { TextField } from '../../shared/ui/TextField'
import { daysBetween, hasFilters, isDate, SORT_FIELDS, SORT_LABELS, type BoardQuery } from './board-query'

const SEARCH_DELAY_MS = 300

const DIRECTION_OPTIONS = [
  { value: 'asc', label: 'Ascending' },
  { value: 'desc', label: 'Descending' },
]

const buttonClass =
  'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'
const clearClass =
  'rounded-md px-2 py-1.5 text-sm font-medium text-sky-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'

interface TaskFiltersProps {
  query: BoardQuery
  /** Longest due-date range allowed by the server; unknown until the settings load. */
  maxRangeDays: number | undefined
  onChange: (changes: Partial<BoardQuery>) => void
}

interface RangeErrors {
  dueFrom?: string
  dueTo?: string
}

/** Search, due-date range and sort controls of the task board (spec 004 FR-014). */
export function TaskFilters({ query, maxRangeDays, onChange }: TaskFiltersProps) {
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
    onChange({ search: '', dueFrom: '', dueTo: '' })
  }

  const rangeHint = maxRangeDays === undefined ? undefined : `Up to ${maxRangeDays} days.`

  return (
    <div role="search" aria-label="Find tasks" className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
      {/* One row that wraps on narrow screens (spec 004 FR-014). Buttons without a label line up with the
          controls through the label's height (mt-5). */}
      <div className="flex flex-wrap items-start gap-3">
        <TextField
          label="Search"
          type="search"
          value={search}
          maxLength={200}
          compact
          fieldClassName="min-w-48 flex-1"
          onChange={(event) => setSearch(event.target.value)}
        />

        <form
          noValidate
          className="contents"
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
            compact
            fieldClassName="w-full sm:w-40"
            onChange={(event) => setDueFrom(event.target.value)}
          />
          <TextField
            label="Due to"
            type="date"
            value={dueTo}
            hint={rangeHint}
            error={rangeErrors.dueTo}
            compact
            fieldClassName="w-full sm:w-40"
            onChange={(event) => setDueTo(event.target.value)}
          />
          <button type="submit" className={`${buttonClass} sm:mt-5`}>
            Apply
          </button>
        </form>

        <SelectField
          label="Sort by"
          options={SORT_FIELDS.map((field) => ({ value: field, label: SORT_LABELS[field] }))}
          value={query.sort}
          compact
          fieldClassName="w-full sm:w-36"
          onChange={(event) => onChange({ sort: event.target.value as BoardQuery['sort'] })}
        />
        <SelectField
          label="Order"
          options={DIRECTION_OPTIONS}
          value={query.direction}
          compact
          fieldClassName="w-full sm:w-36"
          onChange={(event) => onChange({ direction: event.target.value as BoardQuery['direction'] })}
        />
        {hasFilters(query) ? (
          <button type="button" className={`${clearClass} sm:mt-5`} onClick={clear}>
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
