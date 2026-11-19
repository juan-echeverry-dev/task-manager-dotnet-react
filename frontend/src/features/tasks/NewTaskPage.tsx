import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router'
import { useSession } from '../auth/session/useSession'
import { taskMessages, type TaskStatusState } from './messages'
import { EMPTY_TASK_FORM } from './schemas'
import { TaskForm } from './TaskForm'
import { createTask, type TaskInput } from './tasks-api'

/** Create screen (spec US1). Rendered only inside RequireAuth. */
export function NewTaskPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const accessToken = useSession().session?.accessToken ?? ''
  const mutation = useMutation({ mutationFn: (input: TaskInput) => createTask(input, accessToken) })

  const save = async (input: TaskInput) => {
    const task = await mutation.mutateAsync(input)
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    const state: TaskStatusState = { status: taskMessages.created }
    await navigate(`/tasks/${task.id}`, { state })
  }

  return (
    <section className="mx-auto w-full max-w-lg space-y-6">
      <div className="space-y-1">
        <Link to="/tasks" className="text-sm font-medium text-sky-700 underline-offset-4 hover:underline">
          ‹ Back to tasks
        </Link>
        <h1 className="text-2xl font-semibold">New task</h1>
      </div>
      <TaskForm
        defaultValues={EMPTY_TASK_FORM}
        pending={mutation.isPending}
        submitLabel="Create task"
        pendingLabel="Creating…"
        onSubmit={save}
      />
    </section>
  )
}
