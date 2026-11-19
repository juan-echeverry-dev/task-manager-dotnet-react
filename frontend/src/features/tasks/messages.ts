/** User-facing messages of the task screens (contracts/spa-routes.md). */
export const taskMessages = {
  serviceUnavailable: "We couldn't reach the service. Please try again.",
  created: 'Task created.',
  updated: 'Task updated.',
  deleted: 'Task deleted.',
  deleteConfirmation: "Delete this task permanently? This can't be undone.",
  noLongerExists: 'This task no longer exists.',
} as const

/** Navigation state used to show a one-time message on the next screen. */
export interface TaskStatusState {
  /** A confirmation ("Task created."), announced politely. */
  status?: string | undefined
  /** Something the person must notice ("This task no longer exists."), announced as an alert. */
  alert?: string | undefined
}
