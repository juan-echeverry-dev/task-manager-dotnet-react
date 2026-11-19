import { z } from 'zod'
import { TASK_STATUSES } from './tasks-api'

// Mirrors the server rules in specs/002-task-management/data-model.md; the server stays the source
// of truth and its field errors are shown the same way.
export const MIN_DUE_DATE = '2000-01-01'
export const MAX_DUE_DATE = '2099-12-31'
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/
const DUE_DATE_MESSAGE = `Enter a due date between ${MIN_DUE_DATE} and ${MAX_DUE_DATE}.`

export const taskFormSchema = z.object({
  title: z.string().trim().min(1, 'Title is required.').max(200, 'Title must be at most 200 characters.'),
  description: z.string().max(2000, 'Description must be at most 2,000 characters.'),
  status: z.enum(TASK_STATUSES, 'Status must be one of: ToDo, InProgress, Done.'),
  dueDate: z
    .string()
    .refine(
      (value) => value === '' || (DATE_PATTERN.test(value) && value >= MIN_DUE_DATE && value <= MAX_DUE_DATE),
      DUE_DATE_MESSAGE,
    ),
})

/** Form values: empty strings stand for "no description" and "no due date". */
export type TaskFormValues = z.input<typeof taskFormSchema>

export const EMPTY_TASK_FORM: TaskFormValues = { title: '', description: '', status: 'ToDo', dueDate: '' }
