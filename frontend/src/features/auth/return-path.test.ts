import { describe, expect, it } from 'vitest'
import { safeReturnPath } from './return-path'

describe('safeReturnPath', () => {
  it.each([
    ['/profile', '/profile'],
    ['/profile?tab=details', '/profile?tab=details'],
    ['/', '/'],
  ])('accepts the internal route %s', (value, expected) => {
    expect(safeReturnPath(value)).toBe(expected)
  })

  it.each([
    [null],
    [''],
    ['https://evil.example'],
    ['//evil.example'],
    ['/\\evil.example'],
    ['profile'],
    ['/does-not-exist'],
    ['javascript:alert(1)'],
  ])('falls back to the profile for %s', (value) => {
    expect(safeReturnPath(value)).toBe('/profile')
  })
})
