import { parseBoardQuery, serializeBoardQuery, type BoardQuery } from './board-query'

/** The board query string: `''` for the default board, `'?…'` otherwise. */
export function boardSearch(query: BoardQuery): string {
  const search = serializeBoardQuery(query).toString()
  return search ? `?${search}` : ''
}

/**
 * Rebuilds the board query string from an address query string, keeping only the board values, so a
 * value such as the create form's `status` preset never reaches the board address.
 */
export function boardSearchFrom(locationSearch: string): string {
  return boardSearch(parseBoardQuery(new URLSearchParams(locationSearch)))
}

/** A board address (`/tasks`, `/tasks/new`, `/tasks/:id`, …) that keeps the board query string. */
export function boardHref(path: string, search: string): string {
  return `${path}${search}`
}
