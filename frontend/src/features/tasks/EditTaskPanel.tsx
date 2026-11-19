import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { boardHref } from './board-links'
import { taskMessages, type TaskStatusState } from './messages'
import type { TaskFormValues } from './schemas'
import { TaskForm } from './TaskForm'
import { useTaskPanel } from './task-panel-context'
import { TaskNotFound } from './TaskNotFound'
import { updateTask, type Task, type TaskInput } from './tasks-api'
import { useTask } from './useTask'

/**
 * Edit form in the side panel (spec 004 US2, FR-009). Saving and cancelling replace the address with the
 * task's details, so the browser's Back does not return to a submitted form.
 */
export function EditTaskPanel() {
  const { id = '' } = useParams()
  const { titleId, search } = useTaskPanel()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const task = useTask(id)
  const mutation = useMutation({ mutationFn: (input: TaskInput) => updateTask(id, input, task.accessToken) })
  const detailsHref = boardHref(`/tasks/${id}`, search)

  if (task.notFound) return <TaskNotFound titleId={titleId} />

  const save = async (input: TaskInput) => {
    await mutation.mutateAsync(input)
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    const state: TaskStatusState = { status: taskMessages.updated }
    await navigate(detailsHref, { state, replace: true })
  }

  // Deleted in another tab after this form was opened (spec 003 FR-003).
  const handleError = (error: unknown) => {
    if (!(error instanceof ApiError && error.status === 404)) return false
    // Leave first: refreshing the task here would show "Task not found" without the explanation.
    const state: TaskStatusState = { alert: taskMessages.noLongerExists }
    void Promise.resolve(navigate(boardHref('/tasks', search), { state, replace: true })).then(() =>
      queryClient.invalidateQueries({ queryKey: ['tasks'] }),
    )
    return true
  }

  return (
    <div className="space-y-6">
      <h2 id={titleId} className="text-xl font-semibold">
        Edit task
      </h2>

      {task.isPending ? <p className="text-slate-600">Loading task…</p> : null}

      {task.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {task.data ? (
        <>
          <TaskForm
            defaultValues={toFormValues(task.data)}
            pending={mutation.isPending}
            submitLabel="Save changes"
            pendingLabel="Saving…"
            onSubmit={save}
            onError={handleError}
          />
          <Link
            to={detailsHref}
            replace
            className="inline-block text-sm font-medium text-sky-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600"
          >
            Cancel
          </Link>
        </>
      ) : null}
    </div>
  )
}

function toFormValues(task: Task): TaskFormValues {
  return {
    title: task.title,
    description: task.description ?? '',
    status: task.status,
    dueDate: task.dueDate ?? '',
  }
}
