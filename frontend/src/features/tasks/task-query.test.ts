import { describe, expect, it } from 'vitest'
import { defaultTaskQuery, hasFilters, parseTaskQuery, serializeTaskQuery, toApiParams, withChanges } from './task-query'

const parse = (search: string, mode: 'active' | 'deleted' = 'active') => parseTaskQuery(new URLSearchParams(search), mode)

describe('parseTaskQuery', () => {
  it('uses the defaults when the address has no state', () => {
    expect(parse('')).toEqual({
      search: '',
      status: '',
      dueFrom: '',
      dueTo: '',
      sort: 'dueDate',
      direction: 'asc',
      page: 1,
      pageSize: 20,
    })
  })

  it('defaults to the most recently deleted first in the deleted list', () => {
    expect(parse('', 'deleted')).toMatchObject({ sort: 'deletedAt', direction: 'desc' })
  })

  it('reads every valid value', () => {
    expect(
      parse('search=demo&status=Done&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc&page=3&pageSize=50'),
    ).toEqual({
      search: 'demo',
      status: 'Done',
      dueFrom: '2026-12-01',
      dueTo: '2026-12-31',
      sort: 'title',
      direction: 'desc',
      page: 3,
      pageSize: 50,
    })
  })

  it('uses the default direction of the chosen sort field', () => {
    expect(parse('sort=createdAt').direction).toBe('desc')
    expect(parse('sort=title').direction).toBe('asc')
  })

  it.each([
    ['status=Later', { status: '' }],
    ['sort=priority', { sort: 'dueDate' }],
    ['sort=deletedAt', { sort: 'dueDate' }],
    ['direction=up', { direction: 'asc' }],
    ['page=0', { page: 1 }],
    ['page=abc', { page: 1 }],
    ['page=1.5', { page: 1 }],
    ['pageSize=101', { pageSize: 20 }],
    ['dueFrom=2026-12-01', { dueFrom: '', dueTo: '' }],
    ['dueFrom=2026-12-31&dueTo=2026-12-01', { dueFrom: '', dueTo: '' }],
    ['dueFrom=12/01/2026&dueTo=2026-12-31', { dueFrom: '', dueTo: '' }],
  ])('drops the invalid value in %s', (search, expected) => {
    expect(parse(search)).toMatchObject(expected)
  })

  it('accepts sorting by deletion date in the deleted list', () => {
    expect(parse('sort=deletedAt&direction=asc', 'deleted')).toMatchObject({ sort: 'deletedAt', direction: 'asc' })
  })
})

describe('serializeTaskQuery', () => {
  it('omits the defaults', () => {
    expect(serializeTaskQuery(defaultTaskQuery('active'), 'active').toString()).toBe('')
    expect(serializeTaskQuery(defaultTaskQuery('deleted'), 'deleted').toString()).toBe('')
  })

  it('round-trips a full query', () => {
    const search = 'search=demo&status=Done&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&direction=desc&page=3&pageSize=50'

    expect(serializeTaskQuery(parse(search), 'active').toString()).toBe(search)
  })

  it('omits a direction equal to the default of the sort field', () => {
    expect(serializeTaskQuery(parse('sort=createdAt&direction=desc'), 'active').toString()).toBe('sort=createdAt')
    expect(serializeTaskQuery(parse('sort=createdAt&direction=asc'), 'active').toString()).toBe(
      'sort=createdAt&direction=asc',
    )
  })
})

describe('withChanges', () => {
  it('resets the page when the search, a filter or the order changes', () => {
    const query = parse('page=4')

    expect(withChanges(query, { search: 'demo' }).page).toBe(1)
    expect(withChanges(query, { status: 'Done' }).page).toBe(1)
    expect(withChanges(query, { dueFrom: '2026-12-01', dueTo: '2026-12-31' }).page).toBe(1)
    expect(withChanges(query, { sort: 'title' }).page).toBe(1)
  })

  it('keeps the requested page when only the page changes', () => {
    expect(withChanges(parse('search=demo'), { page: 2 })).toMatchObject({ search: 'demo', page: 2 })
  })

  it('resets the direction to the default of a new sort field', () => {
    expect(withChanges(parse('sort=title&direction=desc'), { sort: 'createdAt' }).direction).toBe('desc')
    expect(withChanges(parse('sort=createdAt&direction=asc'), { sort: 'title' }).direction).toBe('asc')
  })
})

describe('hasFilters', () => {
  it('is true only with a search, a status or a range', () => {
    expect(hasFilters(parse('sort=title&page=2'))).toBe(false)
    expect(hasFilters(parse('search=demo'))).toBe(true)
    expect(hasFilters(parse('status=Done'))).toBe(true)
    expect(hasFilters(parse('dueFrom=2026-12-01&dueTo=2026-12-31'))).toBe(true)
  })
})

describe('toApiParams', () => {
  it('sends only the values that differ from the defaults, with the API names', () => {
    expect(toApiParams(parse('search=demo&sort=title&page=2'), 'active')).toEqual({ search: 'demo', sort: 'title', page: '2' })
  })
})
