import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { ConfirmDialog } from '../../shared/ui/ConfirmDialog'
import { OverdueBadge, StatusBadge } from './badges'
import { formatDate, formatTimestamp } from './format'
import { taskMessages, type TaskStatusState } from './messages'
import { isOverdue, localToday } from './overdue'
import { StatusMessage } from './StatusMessage'
import { TaskNotFound } from './TaskNotFound'
import { deleteTask, restoreTask, type Task } from './tasks-api'
import { useTask } from './useTask'

const actionClass =
  'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60'

/**
 * Task detail (spec US2, US3). An active task offers Edit and Delete; a deleted task is read-only and
 * offers only Restore. Rendered only inside RequireAuth.
 */
export function TaskDetailPage() {
  const { id = '' } = useParams()
  const task = useTask(id)
  const [actionError, setActionError] = useState<string>()

  if (task.notFound) return <TaskNotFound />

  return (
    <section className="mx-auto w-full max-w-2xl space-y-6">
      <Link to="/tasks" className="text-sm font-medium text-sky-700 underline-offset-4 hover:underline">
        ‹ Back to tasks
      </Link>
      <StatusMessage />

      {task.isPending ? <p className="text-slate-600">Loading task…</p> : null}

      {task.isError || actionError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {task.data ? (
        <TaskDetails
          task={task.data}
          accessToken={task.accessToken}
          onActionFailed={() => setActionError(taskMessages.serviceUnavailable)}
        />
      ) : null}
    </section>
  )
}

interface TaskDetailsProps {
  task: Task
  accessToken: string
  onActionFailed: () => void
}

function TaskDetails({ task, accessToken, onActionFailed }: TaskDetailsProps) {
  const deleted = task.deletedAt !== null

  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <h1 className="break-words text-2xl font-semibold">{task.title}</h1>
          {task.deletedAt ? (
            <p className="text-sm font-medium text-red-800">Deleted on {formatTimestamp(task.deletedAt)}</p>
          ) : null}
        </div>
        {deleted ? (
          <RestoreAction task={task} accessToken={accessToken} onFailed={onActionFailed} />
        ) : (
          <div className="flex gap-2">
            <Link to={`/tasks/${task.id}/edit`} className={actionClass}>
              Edit
            </Link>
            <DeleteAction task={task} accessToken={accessToken} onFailed={onActionFailed} />
          </div>
        )}
      </div>

      <dl className="divide-y divide-slate-200 rounded-md border border-slate-200 bg-white">
        <DetailRow label="Status">
          <span className="flex flex-wrap items-center gap-2">
            <StatusBadge status={task.status} />
            {!deleted && isOverdue(task, localToday()) ? <OverdueBadge /> : null}
          </span>
        </DetailRow>
        <DetailRow label="Due date">{task.dueDate ? formatDate(task.dueDate) : 'No due date'}</DetailRow>
        <DetailRow label="Description">
          {task.description ? (
            <span className="whitespace-pre-wrap">{task.description}</span>
          ) : (
            <span className="text-slate-500">No description</span>
          )}
        </DetailRow>
        <DetailRow label="Created">{formatTimestamp(task.createdAt)}</DetailRow>
        <DetailRow label="Last updated">{formatTimestamp(task.updatedAt)}</DetailRow>
      </dl>
    </>
  )
}

interface TaskActionProps {
  task: Task
  accessToken: string
  onFailed: () => void
}

function DeleteAction({ task, accessToken, onFailed }: TaskActionProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const mutation = useMutation({ mutationFn: () => deleteTask(task.id, accessToken) })

  const confirm = async () => {
    let state: TaskStatusState = { status: taskMessages.deleted }
    try {
      await mutation.mutateAsync()
    } catch (error) {
      // Already deleted elsewhere: nothing left to show here (spec 003 FR-003).
      if (!(error instanceof ApiError && error.status === 404)) {
        setConfirming(false)
        onFailed()
        return
      }
      state = { alert: taskMessages.noLongerExists }
    }
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    await navigate('/tasks', { state })
  }

  return (
    <>
      <button type="button" className={actionClass} onClick={() => setConfirming(true)}>
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

function RestoreAction({ task, accessToken, onFailed }: TaskActionProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const mutation = useMutation({ mutationFn: () => restoreTask(task.id, accessToken) })

  const restore = async () => {
    try {
      await mutation.mutateAsync()
    } catch {
      onFailed()
      return
    }
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    const state: TaskStatusState = { status: taskMessages.restored }
    await navigate(`/tasks/${task.id}`, { replace: true, state })
  }

  return (
    <button
      type="button"
      className={actionClass}
      onClick={() => void restore()}
      disabled={mutation.isPending}
      aria-busy={mutation.isPending}
    >
      {mutation.isPending ? 'Restoring…' : 'Restore'}
    </button>
  )
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1 px-4 py-3 sm:flex-row sm:gap-4">
      <dt className="text-sm font-medium text-slate-500 sm:w-36 sm:shrink-0">{label}</dt>
      <dd className="min-w-0 break-words text-slate-900">{children}</dd>
    </div>
  )
}
