import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { SelectField } from './SelectField'

const options = [
  { value: 'ToDo', label: 'To do' },
  { value: 'Done', label: 'Done' },
]

describe('SelectField', () => {
  it('labels the select and renders its options', () => {
    render(<SelectField label="Status" options={options} defaultValue="Done" />)

    const select = screen.getByLabelText('Status')
    expect(select).toHaveValue('Done')
    expect(screen.getByRole('option', { name: 'To do' })).toHaveValue('ToDo')
    expect(select).not.toHaveAttribute('aria-invalid')
  })

  it('associates the error message with the select', () => {
    render(<SelectField label="Status" options={options} error="Status is required." />)

    const select = screen.getByLabelText('Status')
    expect(select).toHaveAttribute('aria-invalid', 'true')
    expect(select).toHaveAccessibleDescription('Status is required.')
  })
})
