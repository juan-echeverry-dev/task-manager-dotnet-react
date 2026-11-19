import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { ConfirmDialog } from './ConfirmDialog'

function Harness({ onConfirm = vi.fn(), pending = false }: { onConfirm?: () => void; pending?: boolean }) {
  const [open, setOpen] = useState(false)
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Delete
      </button>
      <ConfirmDialog
        open={open}
        title="Delete task"
        message="Delete this task permanently? This can't be undone."
        confirmLabel="Delete"
        pendingLabel="Deleting…"
        pending={pending}
        onConfirm={onConfirm}
        onCancel={() => setOpen(false)}
      />
    </>
  )
}

describe('ConfirmDialog', () => {
  it('is closed until opened', () => {
    render(<Harness />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('opens as a dialog with its title, message, and both actions', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const dialog = screen.getByRole('dialog', { name: 'Delete task' })
    expect(dialog).toHaveAccessibleDescription("Delete this task permanently? This can't be undone.")
    expect(within(dialog).getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus()
  })

  it('confirms with the confirm action', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(<Harness onConfirm={onConfirm} />)
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(onConfirm).toHaveBeenCalledOnce()
  })

  it.each([
    ['Cancel', async (user: ReturnType<typeof userEvent.setup>) => user.click(screen.getByRole('button', { name: 'Cancel' }))],
    ['Escape', async (user: ReturnType<typeof userEvent.setup>) => user.keyboard('{Escape}')],
  ])('closes without confirming on %s and returns focus to the trigger', async (_name, dismiss) => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(<Harness onConfirm={onConfirm} />)
    const trigger = screen.getByRole('button', { name: 'Delete' })
    await user.click(trigger)

    await dismiss(user)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(onConfirm).not.toHaveBeenCalled()
    expect(trigger).toHaveFocus()
  })

  it('disables the confirm action while pending', async () => {
    const user = userEvent.setup()
    render(<Harness pending />)

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const confirm = within(screen.getByRole('dialog')).getByRole('button', { name: 'Deleting…' })
    expect(confirm).toBeDisabled()
    expect(confirm).toHaveAttribute('aria-busy', 'true')
  })
})
