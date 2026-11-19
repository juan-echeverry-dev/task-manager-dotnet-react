import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { taskMessages, type TaskStatusState } from './messages'
import type { TaskFormValues } from './schemas'
import { TaskForm } from './TaskForm'
import { TaskNotFound } from './TaskNotFound'
import { updateTask, type Task, type TaskInput } from './tasks-api'
import { useTask } from './useTask'

/** Edit screen (spec US2). Rendered only inside RequireAuth. */
export function EditTaskPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const task = useTask(id)
  const mutation = useMutation({ mutationFn: (input: TaskInput) => updateTask(id, input, task.accessToken) })

  if (task.notFound) return <TaskNotFound />

  const save = async (input: TaskInput) => {
    await mutation.mutateAsync(input)
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    const state: TaskStatusState = { status: taskMessages.updated }
    await navigate(`/tasks/${id}`, { state })
  }

  // Deleted in another tab after this form was opened (spec 003 FR-003).
  const handleError = (error: unknown) => {
    if (!(error instanceof ApiError && error.status === 404)) return false
    // Leave first: refreshing the task here would show "Task not found" without the explanation.
    const state: TaskStatusState = { alert: taskMessages.noLongerExists }
    void Promise.resolve(navigate('/tasks', { state })).then(() => queryClient.invalidateQueries({ queryKey: ['tasks'] }))
    return true
  }

  return (
    <section className="mx-auto w-full max-w-lg space-y-6">
      <div className="space-y-1">
        <Link to={`/tasks/${id}`} className="text-sm font-medium text-sky-700 underline-offset-4 hover:underline">
          ‹ Back to the task
        </Link>
        <h1 className="text-2xl font-semibold">Edit task</h1>
      </div>

      {task.isPending ? <p className="text-slate-600">Loading task…</p> : null}

      {task.isError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {taskMessages.serviceUnavailable}
        </p>
      ) : null}

      {task.data ? (
        <TaskForm
          defaultValues={toFormValues(task.data)}
          pending={mutation.isPending}
          submitLabel="Save changes"
          pendingLabel="Saving…"
          onSubmit={save}
          onError={handleError}
        />
      ) : null}
    </section>
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
