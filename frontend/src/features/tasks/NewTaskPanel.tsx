import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router'
import { useSession } from '../auth/session/useSession'
import { boardHref } from './board-links'
import { taskMessages, type TaskStatusState } from './messages'
import { EMPTY_TASK_FORM } from './schemas'
import { useTaskPanel } from './task-panel-context'
import { TaskForm } from './TaskForm'
import { createTask, TASK_STATUSES, type TaskInput } from './tasks-api'

/**
 * Create form in the side panel (spec 004 US3, FR-008). `?status=` presets the status (a column's "+");
 * anything else falls back to To do. On success the panel shows the new task, replacing the form's address.
 */
export function NewTaskPanel() {
  const { titleId, search } = useTaskPanel()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const accessToken = useSession().session?.accessToken ?? ''
  const mutation = useMutation({ mutationFn: (input: TaskInput) => createTask(input, accessToken) })
  const preset = TASK_STATUSES.find((status) => status === searchParams.get('status')) ?? 'ToDo'

  const save = async (input: TaskInput) => {
    const task = await mutation.mutateAsync(input)
    await queryClient.invalidateQueries({ queryKey: ['tasks'] })
    const state: TaskStatusState = { status: taskMessages.created }
    await navigate(boardHref(`/tasks/${task.id}`, search), { state, replace: true })
  }

  return (
    <div className="space-y-6">
      <h2 id={titleId} className="text-xl font-semibold">
        New task
      </h2>
      <TaskForm
        defaultValues={{ ...EMPTY_TASK_FORM, status: preset }}
        pending={mutation.isPending}
        submitLabel="Create task"
        pendingLabel="Creating…"
        onSubmit={save}
      />
    </div>
  )
}
