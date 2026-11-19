import { afterEach, describe, expect, it, vi } from 'vitest'
import { isOverdue, localToday } from './overdue'

describe('isOverdue', () => {
  const today = '2026-11-18'

  it('is true for an unfinished task due before today', () => {
    expect(isOverdue({ status: 'ToDo', dueDate: '2026-11-17' }, today)).toBe(true)
    expect(isOverdue({ status: 'InProgress', dueDate: '2025-01-01' }, today)).toBe(true)
  })

  it('is false for a task due today or later', () => {
    expect(isOverdue({ status: 'ToDo', dueDate: '2026-11-18' }, today)).toBe(false)
    expect(isOverdue({ status: 'ToDo', dueDate: '2026-11-19' }, today)).toBe(false)
  })

  it('is false for a Done task, even when its due date has passed', () => {
    expect(isOverdue({ status: 'Done', dueDate: '2026-11-01' }, today)).toBe(false)
  })

  it('is false for a task without a due date', () => {
    expect(isOverdue({ status: 'ToDo', dueDate: null }, today)).toBe(false)
  })
})

describe('localToday', () => {
  afterEach(() => vi.useRealTimers())

  it('uses the local calendar date, not the UTC date', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 18, 23, 30))

    expect(localToday()).toBe('2026-11-18')
  })
})
