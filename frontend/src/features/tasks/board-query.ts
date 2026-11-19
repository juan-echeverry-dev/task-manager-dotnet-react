import type { TaskStatus } from './tasks-api'

/**
 * Board state kept in the address (spec 004 FR-016, data-model.md "BoardQuery"). Names match the API
 * query parameters; defaults are left out of the address, invalid values fall back to the defaults,
 * and the former list values (`status`, `page`, `pageSize`) are ignored.
 */
export type SortField = 'dueDate' | 'createdAt' | 'title'
export type SortDirection = 'asc' | 'desc'

export interface BoardQuery {
  search: string
  dueFrom: string
  dueTo: string
  sort: SortField
  direction: SortDirection
}

export const SORT_LABELS: Record<SortField, string> = {
  dueDate: 'Due date',
  createdAt: 'Created',
  title: 'Title',
}

export const SORT_FIELDS: readonly SortField[] = ['dueDate', 'createdAt', 'title']

/** How many tasks a column loads at a time (spec 004 FR-006). */
export const BOARD_PAGE_SIZE = 20

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

/** Creation dates read newest first; due dates and titles read ascending. */
export function defaultDirection(sort: SortField): SortDirection {
  return sort === 'createdAt' ? 'desc' : 'asc'
}

export function defaultBoardQuery(): BoardQuery {
  return { search: '', dueFrom: '', dueTo: '', sort: 'dueDate', direction: 'asc' }
}

export function parseBoardQuery(params: URLSearchParams): BoardQuery {
  const sort = params.get('sort')
  const parsedSort = SORT_FIELDS.find((field) => field === sort) ?? 'dueDate'
  const direction = params.get('direction')
  const dueFrom = params.get('dueFrom') ?? ''
  const dueTo = params.get('dueTo') ?? ''
  const validRange = isDate(dueFrom) && isDate(dueTo) && dueFrom <= dueTo

  return {
    search: params.get('search')?.trim() ?? '',
    dueFrom: validRange ? dueFrom : '',
    dueTo: validRange ? dueTo : '',
    sort: parsedSort,
    direction: direction === 'asc' || direction === 'desc' ? direction : defaultDirection(parsedSort),
  }
}

export function serializeBoardQuery(query: BoardQuery): URLSearchParams {
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.dueFrom && query.dueTo) {
    params.set('dueFrom', query.dueFrom)
    params.set('dueTo', query.dueTo)
  }
  if (query.sort !== 'dueDate') params.set('sort', query.sort)
  if (query.direction !== defaultDirection(query.sort)) params.set('direction', query.direction)
  return params
}

/** Applies changes from the screen; a new sort field starts with its own default direction. */
export function withChanges(query: BoardQuery, changes: Partial<BoardQuery>): BoardQuery {
  const next = { ...query, ...changes }
  if (changes.sort !== undefined && changes.direction === undefined) next.direction = defaultDirection(changes.sort)
  return next
}

export function hasFilters(query: BoardQuery): boolean {
  return query.search !== '' || (query.dueFrom !== '' && query.dueTo !== '')
}

/** API parameters of one column's first batch; the column adds `page` for "Load more". */
export function columnParams(query: BoardQuery, status: TaskStatus): Record<string, string> {
  return { status, pageSize: String(BOARD_PAGE_SIZE), ...Object.fromEntries(serializeBoardQuery(query)) }
}

export function isDate(value: string): boolean {
  return DATE_PATTERN.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`)) && toDayNumber(value) !== null
}

/** Whole days from one `yyyy-MM-dd` date to another. */
export function daysBetween(from: string, to: string): number {
  return (toDayNumber(to) ?? 0) - (toDayNumber(from) ?? 0)
}

function toDayNumber(value: string): number | null {
  const [year, month, day] = value.split('-').map(Number)
  const time = Date.UTC(year ?? 0, (month ?? 1) - 1, day ?? 1)
  const date = new Date(time)
  // Rejects dates such as 2026-02-30, which Date would roll over.
  if (date.getUTCFullYear() !== year || date.getUTCMonth() + 1 !== month || date.getUTCDate() !== day) return null
  return time / 86_400_000
}
