import { useEffect, useId, useRef } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router'
import { boardHref, boardSearchFrom } from './board-links'
import type { TaskPanelContext } from './task-panel-context'

/**
 * Side panel over the task board (spec 004 US2, research R4). A modal native <dialog>: the browser
 * traps focus and makes the board inert. Close and Escape replace the address with the board, so the
 * browser's Back never reopens a closed panel; Back while open closes it like any navigation.
 */
export function TaskPanel() {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const closeRef = useRef<HTMLButtonElement>(null)
  const titleId = useId()
  const navigate = useNavigate()
  const search = boardSearchFrom(useLocation().search)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null
    if (!dialog.open) dialog.showModal()
    closeRef.current?.focus()

    return () => {
      if (dialog.open) dialog.close()
      // Back to the card that opened the panel; when it is gone (deleted, filtered), to the board heading.
      const target = opener?.isConnected && opener !== document.body ? opener : document.querySelector<HTMLElement>('main h1')
      target?.focus()
    }
  }, [])

  const close = () => void navigate(boardHref('/tasks', search), { replace: true })

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      onCancel={(event) => {
        // Escape: navigate instead of letting the browser close it, so the address and the panel agree.
        event.preventDefault()
        close()
      }}
      className="fixed inset-y-0 right-0 left-auto m-0 h-dvh max-h-none w-full max-w-none overflow-y-auto border-l border-slate-200 bg-white p-0 text-slate-900 shadow-2xl backdrop:bg-slate-900/20 sm:w-[32rem]"
    >
      <div className="flex justify-end px-4 pt-4">
        <button
          ref={closeRef}
          type="button"
          onClick={close}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
        >
          Close
        </button>
      </div>
      <div className="px-6 pb-8">
        <Outlet context={{ titleId, search } satisfies TaskPanelContext} />
      </div>
    </dialog>
  )
}
