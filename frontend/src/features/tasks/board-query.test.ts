import { describe, expect, it } from 'vitest'
import {
  BOARD_PAGE_SIZE,
  columnParams,
  defaultBoardQuery,
  hasFilters,
  parseBoardQuery,
  serializeBoardQuery,
  withChanges,
} from './board-query'

const parse = (search: string) => parseBoardQuery(new URLSearchParams(search))

describe('parseBoardQuery', () => {
  it('uses the defaults when the address has no state', () => {
    expect(parse('')).toEqual({ search: '', dueFrom: '', dueTo: '', sort: 'dueDate', direction: 'asc' })
  })

  it('reads every valid value', () => {
    expect(parse('search=demo&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc')).toEqual({
      search: 'demo',
      dueFrom: '2026-12-01',
      dueTo: '2026-12-31',
      sort: 'title',
      direction: 'desc',
    })
  })

  it('ignores the former list values status, page and page size', () => {
    expect(parse('status=Done&page=3&pageSize=50')).toEqual(defaultBoardQuery())
  })

  it('uses the default direction of the chosen sort field', () => {
    expect(parse('sort=createdAt').direction).toBe('desc')
    expect(parse('sort=title').direction).toBe('asc')
  })

  it.each([
    ['sort=priority', { sort: 'dueDate' }],
    ['sort=deletedAt', { sort: 'dueDate' }],
    ['direction=up', { direction: 'asc' }],
    ['dueFrom=2026-12-01', { dueFrom: '', dueTo: '' }],
    ['dueFrom=2026-12-31&dueTo=2026-12-01', { dueFrom: '', dueTo: '' }],
    ['dueFrom=12/01/2026&dueTo=2026-12-31', { dueFrom: '', dueTo: '' }],
  ])('drops the invalid value in %s', (search, expected) => {
    expect(parse(search)).toMatchObject(expected)
  })
})

describe('serializeBoardQuery', () => {
  it('omits the defaults', () => {
    expect(serializeBoardQuery(defaultBoardQuery()).toString()).toBe('')
  })

  it('round-trips a full query', () => {
    const search = 'search=demo&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc'

    expect(serializeBoardQuery(parse(search)).toString()).toBe(search)
  })

  it('omits a direction equal to the default of the sort field', () => {
    expect(serializeBoardQuery(parse('sort=createdAt&direction=desc')).toString()).toBe('sort=createdAt')
    expect(serializeBoardQuery(parse('sort=createdAt&direction=asc')).toString()).toBe('sort=createdAt&direction=asc')
  })
})

describe('withChanges', () => {
  it('applies the changes', () => {
    expect(withChanges(parse('sort=title'), { search: 'demo' })).toMatchObject({ search: 'demo', sort: 'title' })
  })

  it('resets the direction to the default of a new sort field', () => {
    expect(withChanges(parse('sort=title&direction=desc'), { sort: 'createdAt' }).direction).toBe('desc')
    expect(withChanges(parse('sort=createdAt&direction=asc'), { sort: 'title' }).direction).toBe('asc')
  })
})

describe('hasFilters', () => {
  it('is true only with a search or a range', () => {
    expect(hasFilters(parse('sort=title'))).toBe(false)
    expect(hasFilters(parse('search=demo'))).toBe(true)
    expect(hasFilters(parse('dueFrom=2026-12-01&dueTo=2026-12-31'))).toBe(true)
  })
})

describe('columnParams', () => {
  it('asks the API for one status, one batch, and the non-default board values', () => {
    expect(BOARD_PAGE_SIZE).toBe(20)
    expect(columnParams(parse('search=demo&sort=title'), 'InProgress')).toEqual({
      status: 'InProgress',
      pageSize: '20',
      search: 'demo',
      sort: 'title',
    })
    expect(columnParams(defaultBoardQuery(), 'ToDo')).toEqual({ status: 'ToDo', pageSize: '20' })
  })
})
