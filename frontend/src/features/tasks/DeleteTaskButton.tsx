import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'
import { boardHref, boardSearchFrom } from './board-links'
import { taskMessages, type TaskStatusState } from './messages'
import { deleteTask, type Task } from './tasks-api'

interface DeleteTaskButtonProps {
  task: Pick<Task, 'id' | 'title'>
  accessToken: string
  /** Called when the delete fails for a reason other than "task not found". */
  onFailed: () => void
  /** Accessible name when the visible "Delete" is ambiguous (for example on a card). */
  label?: string | undefined
  className: string
}

/**
 * Permanent deletion with confirmation (spec 003, spec 004 FR-013). On success, or when the task no
 * longer exists, it returns to the board with the matching message, replacing the current address.
 */
export function DeleteTaskButton({ task, accessToken, onFailed, label, className }: DeleteTaskButtonProps) {
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const mutation = useMutation({ mutationFn: () => deleteTask(task.id, accessToken) })

  const confirm = async () => {
    let state: TaskStatusState = { status: taskMessages.deleted }
    try {
      await mutation.mutateAsync()
    } catch (error) {
      // Already deleted elsewhere: nothing left to show (spec 003 FR-003).
      if (!(error instanceof ApiError && error.status === 404)) {
        setConfirming(false)
        onFailed()
        return
      }
      state = { alert: taskMessages.noLongerExists }
    }
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    await navigate(boardHref('/tasks', boardSearchFrom(location.search)), { state, replace: true })
  }

  return (
    <>
      <button type="button" aria-label={label} className={className} onClick={() => setConfirming(true)}>
        Delete
      </button>
      <ConfirmDialog
        open={confirming}
        title="Delete task"
        message={taskMessages.deleteConfirmation}
        confirmLabel="Delete"
        pendingLabel="Deleting…"
        pending={mutation.isPending}
        onConfirm={() => void confirm()}
        onCancel={() => setConfirming(false)}
      />
    </>
  )
}
