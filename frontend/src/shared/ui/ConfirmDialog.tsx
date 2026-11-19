import { useEffect, useId, useRef } from 'react'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel: string
  pendingLabel: string
  pending: boolean
  onConfirm: () => void
  onCancel: () => void
}

const buttonClass =
  'rounded-md px-4 py-2 text-sm font-medium focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60'

/**
 * Modal confirmation built on the native <dialog>: the browser traps focus and closes it on Escape.
 * Focus returns to the element that opened it (contracts/spa-routes.md, accessibility).
 */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  pendingLabel,
  pending,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const returnFocusRef = useRef<HTMLElement | null>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const titleId = useId()
  const messageId = useId()

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) {
      returnFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null
      dialog.showModal()
      // Start on the safe choice, so Enter does not delete by accident.
      cancelRef.current?.focus()
    } else if (!open && dialog.open) {
      dialog.close()
      returnFocusRef.current?.focus()
    }
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      aria-describedby={messageId}
      onCancel={(event) => {
        // Escape: let React state decide, so the dialog and `open` never disagree.
        event.preventDefault()
        if (!pending) onCancel()
      }}
      className="m-auto w-[min(28rem,calc(100vw-2rem))] rounded-lg border border-slate-200 bg-white p-6 text-slate-900 shadow-xl backdrop:bg-slate-900/40"
    >
      {open ? (
        <div className="space-y-4">
          <h2 id={titleId} className="text-lg font-semibold">
            {title}
          </h2>
          <p id={messageId} className="text-slate-700">
            {message}
          </p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              ref={cancelRef}
              type="button"
              onClick={onCancel}
              disabled={pending}
              className={`${buttonClass} border border-slate-300 bg-white text-slate-700 hover:bg-slate-100`}
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={pending}
              aria-busy={pending}
              className={`${buttonClass} bg-red-700 text-white hover:bg-red-800`}
            >
              {pending ? pendingLabel : confirmLabel}
            </button>
          </div>
        </div>
      ) : null}
    </dialog>
  )
}
