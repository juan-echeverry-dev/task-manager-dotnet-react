import { TASK_STATUSES, type TaskStatus } from './tasks-api'

/**
 * List state kept in the address (spec FR-019, contracts/spa-routes.md). Names match the API query
 * parameters; defaults are left out of the address, and invalid values fall back to the defaults.
 */
export type TaskListMode = 'active' | 'deleted'
export type SortField = 'dueDate' | 'createdAt' | 'title' | 'deletedAt'
export type SortDirection = 'asc' | 'desc'

export interface TaskQuery {
  search: string
  status: TaskStatus | ''
  dueFrom: string
  dueTo: string
  sort: SortField
  direction: SortDirection
  page: number
  pageSize: number
}

export const SORT_LABELS: Record<SortField, string> = {
  dueDate: 'Due date',
  createdAt: 'Created',
  title: 'Title',
  deletedAt: 'Deleted on',
}

const DEFAULT_PAGE_SIZE = 20
const MAX_PAGE_SIZE = 100
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

export function sortFields(mode: TaskListMode): SortField[] {
  return mode === 'deleted' ? ['dueDate', 'createdAt', 'title', 'deletedAt'] : ['dueDate', 'createdAt', 'title']
}

/** Creation and deletion dates read newest first; due dates and titles read ascending. */
export function defaultDirection(sort: SortField): SortDirection {
  return sort === 'createdAt' || sort === 'deletedAt' ? 'desc' : 'asc'
}

export function defaultTaskQuery(mode: TaskListMode): TaskQuery {
  const sort: SortField = mode === 'deleted' ? 'deletedAt' : 'dueDate'
  return {
    search: '',
    status: '',
    dueFrom: '',
    dueTo: '',
    sort,
    direction: defaultDirection(sort),
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  }
}

export function parseTaskQuery(params: URLSearchParams, mode: TaskListMode): TaskQuery {
  const defaults = defaultTaskQuery(mode)
  const status = params.get('status')
  const sort = params.get('sort')
  const parsedSort = sortFields(mode).find((field) => field === sort) ?? defaults.sort
  const direction = params.get('direction')
  const dueFrom = params.get('dueFrom') ?? ''
  const dueTo = params.get('dueTo') ?? ''
  const validRange = isDate(dueFrom) && isDate(dueTo) && dueFrom <= dueTo

  return {
    search: params.get('search')?.trim() ?? '',
    status: TASK_STATUSES.find((name) => name === status) ?? '',
    dueFrom: validRange ? dueFrom : '',
    dueTo: validRange ? dueTo : '',
    sort: parsedSort,
    direction: direction === 'asc' || direction === 'desc' ? direction : defaultDirection(parsedSort),
    page: parseInteger(params.get('page'), 1, Number.MAX_SAFE_INTEGER) ?? 1,
    pageSize: parseInteger(params.get('pageSize'), 1, MAX_PAGE_SIZE) ?? DEFAULT_PAGE_SIZE,
  }
}

export function serializeTaskQuery(query: TaskQuery, mode: TaskListMode): URLSearchParams {
  const defaults = defaultTaskQuery(mode)
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.dueFrom && query.dueTo) {
    params.set('dueFrom', query.dueFrom)
    params.set('dueTo', query.dueTo)
  }
  if (query.sort !== defaults.sort) params.set('sort', query.sort)
  if (query.direction !== defaultDirection(query.sort)) params.set('direction', query.direction)
  if (query.page !== 1) params.set('page', String(query.page))
  if (query.pageSize !== DEFAULT_PAGE_SIZE) params.set('pageSize', String(query.pageSize))
  return params
}

/**
 * Applies changes from the screen. Anything but the page sends the person back to page 1, and a new
 * sort field starts with its own default direction.
 */
export function withChanges(query: TaskQuery, changes: Partial<TaskQuery>): TaskQuery {
  const next = { ...query, ...changes }
  if (changes.sort !== undefined && changes.direction === undefined) next.direction = defaultDirection(changes.sort)
  if (changes.page === undefined) next.page = 1
  return next
}

export function hasFilters(query: TaskQuery): boolean {
  return query.search !== '' || query.status !== '' || (query.dueFrom !== '' && query.dueTo !== '')
}

export function toApiParams(query: TaskQuery, mode: TaskListMode): Record<string, string> {
  return Object.fromEntries(serializeTaskQuery(query, mode))
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

function parseInteger(value: string | null, min: number, max: number): number | undefined {
  if (value === null || !/^\d+$/.test(value)) return undefined
  const number = Number(value)
  return number >= min && number <= max ? number : undefined
}
