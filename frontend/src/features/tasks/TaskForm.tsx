import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '../../shared/api/http-client'
import { SelectField } from '../../shared/ui/SelectField'
import { SubmitButton } from '../../shared/ui/SubmitButton'
import { TextAreaField } from '../../shared/ui/TextAreaField'
import { TextField } from '../../shared/ui/TextField'
import { STATUS_LABELS } from './format'
import { taskMessages } from './messages'
import { MAX_DUE_DATE, MIN_DUE_DATE, taskFormSchema, type TaskFormValues } from './schemas'
import { TASK_STATUSES, type TaskInput } from './tasks-api'

const FIELDS = ['title', 'description', 'status', 'dueDate'] as const
const STATUS_OPTIONS = TASK_STATUSES.map((status) => ({ value: status, label: STATUS_LABELS[status] }))

interface TaskFormProps {
  defaultValues: TaskFormValues
  pending: boolean
  submitLabel: string
  pendingLabel: string
  /** Saves the task. Rejections with field errors or other failures are shown by the form. */
  onSubmit: (input: TaskInput) => Promise<void>
  /** Handles failures the form does not know about; returns true when it did. */
  onError?: ((error: unknown) => boolean) | undefined
}

/** Create and edit form (contracts/spa-routes.md). Values are kept when saving fails. */
export function TaskForm({ defaultValues, pending, submitLabel, pendingLabel, onSubmit, onError }: TaskFormProps) {
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<TaskFormValues>({ resolver: zodResolver(taskFormSchema), defaultValues })

  const submit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      await onSubmit(toInput(values))
    } catch (error) {
      if (onError?.(error)) return
      if (error instanceof ApiError && error.status === 401) return
      if (error instanceof ApiError && error.status === 400 && error.problem?.errors) {
        for (const field of FIELDS) {
          const message = error.problem.errors[field]?.[0]
          if (message) setError(field, { message }, { shouldFocus: true })
        }
      } else {
        setFormError(taskMessages.serviceUnavailable)
      }
    }
  })

  return (
    <form noValidate onSubmit={submit} className="space-y-4">
      {formError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {formError}
        </p>
      ) : null}
      <TextField label="Title" autoComplete="off" error={errors.title?.message} {...register('title')} />
      <TextAreaField label="Description" error={errors.description?.message} {...register('description')} />
      <SelectField label="Status" options={STATUS_OPTIONS} error={errors.status?.message} {...register('status')} />
      <TextField
        label="Due date"
        type="date"
        min={MIN_DUE_DATE}
        max={MAX_DUE_DATE}
        error={errors.dueDate?.message}
        {...register('dueDate')}
      />
      <SubmitButton pending={pending} label={submitLabel} pendingLabel={pendingLabel} />
    </form>
  )
}

function toInput(values: TaskFormValues): TaskInput {
  return {
    title: values.title.trim(),
    description: values.description.trim() === '' ? null : values.description,
    status: values.status,
    dueDate: values.dueDate === '' ? null : values.dueDate,
  }
}
