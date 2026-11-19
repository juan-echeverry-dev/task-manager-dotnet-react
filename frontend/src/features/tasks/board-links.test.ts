import { describe, expect, it } from 'vitest'
import { parseBoardQuery } from './board-query'
import { boardHref, boardSearch, boardSearchFrom } from './board-links'

describe('boardSearch', () => {
  it('is empty for the default board and starts with "?" otherwise', () => {
    expect(boardSearch(parseBoardQuery(new URLSearchParams()))).toBe('')
    expect(boardSearch(parseBoardQuery(new URLSearchParams('search=demo&sort=title')))).toBe('?search=demo&sort=title')
  })
})

describe('boardSearchFrom', () => {
  it('keeps only the board values of an address query string', () => {
    expect(boardSearchFrom('?status=InProgress&search=demo')).toBe('?search=demo')
    expect(boardSearchFrom('?page=3&pageSize=50&other=1')).toBe('')
    expect(boardSearchFrom('')).toBe('')
  })
})

describe('boardHref', () => {
  it('appends the board query string to a path', () => {
    expect(boardHref('/tasks/abc', '?search=demo')).toBe('/tasks/abc?search=demo')
    expect(boardHref('/tasks', '')).toBe('/tasks')
  })
})
