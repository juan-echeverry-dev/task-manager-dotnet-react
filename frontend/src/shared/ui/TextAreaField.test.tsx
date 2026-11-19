import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TextAreaField } from './TextAreaField'

describe('TextAreaField', () => {
  it('labels the text area', () => {
    render(<TextAreaField label="Description" defaultValue="Slides" />)

    const textArea = screen.getByLabelText('Description')
    expect(textArea.tagName).toBe('TEXTAREA')
    expect(textArea).toHaveValue('Slides')
    expect(textArea).not.toHaveAttribute('aria-invalid')
  })

  it('associates the error message with the text area', () => {
    render(<TextAreaField label="Description" error="Description must be at most 2,000 characters." />)

    const textArea = screen.getByLabelText('Description')
    expect(textArea).toHaveAttribute('aria-invalid', 'true')
    expect(textArea).toHaveAccessibleDescription('Description must be at most 2,000 characters.')
  })
})
