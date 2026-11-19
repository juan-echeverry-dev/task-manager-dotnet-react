import { act, fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defaultBoardQuery, type BoardQuery } from './board-query'
import { TaskFilters } from './TaskFilters'

function renderFilters(query: Partial<BoardQuery> = {}) {
  const onChange = vi.fn()
  render(<TaskFilters query={{ ...defaultBoardQuery(), ...query }} maxRangeDays={90} onChange={onChange} />)
  return { onChange, user: userEvent.setup() }
}

describe('TaskFilters', () => {
  afterEach(() => vi.useRealTimers())

  it('renders labelled controls with the current values and the range hint', () => {
    renderFilters({ search: 'demo', dueFrom: '2026-12-01', dueTo: '2026-12-31', sort: 'title' })

    expect(screen.getByLabelText('Search')).toHaveValue('demo')
    expect(screen.queryByLabelText('Status')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Due from')).toHaveValue('2026-12-01')
    expect(screen.getByLabelText('Due to')).toHaveValue('2026-12-31')
    expect(screen.getByLabelText('Due to')).toHaveAccessibleDescription('Up to 90 days.')
    expect(screen.getByLabelText('Sort by')).toHaveValue('title')
    expect(screen.getByLabelText('Order')).toHaveValue('asc')
  })

  it('offers sorting by due date, creation date and title only', () => {
    renderFilters()

    const options = Array.from((screen.getByLabelText('Sort by') as HTMLSelectElement).options).map((option) => option.text)
    expect(options).toEqual(['Due date', 'Created', 'Title'])
  })

  it('applies the search after the person stops typing for 300 ms', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    const onChange = vi.fn()
    render(<TaskFilters query={defaultBoardQuery()} maxRangeDays={90} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Search'), { target: { value: 'dem' } })
    act(() => vi.advanceTimersByTime(200))
    fireEvent.change(screen.getByLabelText('Search'), { target: { value: 'demo' } })
    act(() => vi.advanceTimersByTime(299))
    expect(onChange).not.toHaveBeenCalled()

    act(() => vi.advanceTimersByTime(1))
    expect(onChange).toHaveBeenCalledExactlyOnceWith({ search: 'demo' })
  })

  it('applies a sort field and a direction', async () => {
    const { onChange, user } = renderFilters()

    await user.selectOptions(screen.getByLabelText('Sort by'), 'createdAt')
    await user.selectOptions(screen.getByLabelText('Order'), 'desc')

    expect(onChange).toHaveBeenNthCalledWith(1, { sort: 'createdAt' })
    expect(onChange).toHaveBeenNthCalledWith(2, { direction: 'desc' })
  })

  it('applies a valid due-date range', async () => {
    const { onChange, user } = renderFilters()

    await user.type(screen.getByLabelText('Due from'), '2026-01-01')
    await user.type(screen.getByLabelText('Due to'), '2026-04-01')
    await user.click(screen.getByRole('button', { name: 'Apply' }))

    expect(onChange).toHaveBeenCalledExactlyOnceWith({ dueFrom: '2026-01-01', dueTo: '2026-04-01' })
  })

  it.each([
    ['2026-01-01', '2026-04-02', 'Due to', 'Choose a range of at most 90 days.'],
    ['2026-02-01', '2026-01-01', 'Due to', 'The start date must be on or before the end date.'],
    ['2026-01-01', '', 'Due to', 'Choose both a start and an end date.'],
    ['', '2026-01-01', 'Due from', 'Choose both a start and an end date.'],
  ])('does not apply the range %s → %s and explains why', async (dueFrom, dueTo, field, message) => {
    const { onChange, user } = renderFilters()
    if (dueFrom) await user.type(screen.getByLabelText('Due from'), dueFrom)
    if (dueTo) await user.type(screen.getByLabelText('Due to'), dueTo)

    await user.click(screen.getByRole('button', { name: 'Apply' }))

    expect(screen.getByLabelText(field)).toHaveAccessibleDescription(expect.stringContaining(message))
    expect(onChange).not.toHaveBeenCalled()
  })

  it('removes the range when both dates are cleared', async () => {
    const { onChange, user } = renderFilters({ dueFrom: '2026-12-01', dueTo: '2026-12-31' })

    await user.clear(screen.getByLabelText('Due from'))
    await user.clear(screen.getByLabelText('Due to'))
    await user.click(screen.getByRole('button', { name: 'Apply' }))

    expect(onChange).toHaveBeenCalledExactlyOnceWith({ dueFrom: '', dueTo: '' })
  })

  it('clears the search and every filter', async () => {
    const { onChange, user } = renderFilters({ search: 'demo', dueFrom: '2026-12-01', dueTo: '2026-12-31' })

    await user.click(screen.getByRole('button', { name: 'Clear search and filters' }))

    expect(onChange).toHaveBeenCalledExactlyOnceWith({ search: '', dueFrom: '', dueTo: '' })
    expect(screen.getByLabelText('Search')).toHaveValue('')
    expect(screen.getByLabelText('Due from')).toHaveValue('')
  })

  it('hides the clear action when nothing is filtered', () => {
    renderFilters({ sort: 'title' })

    expect(screen.queryByRole('button', { name: 'Clear search and filters' })).not.toBeInTheDocument()
  })
})
