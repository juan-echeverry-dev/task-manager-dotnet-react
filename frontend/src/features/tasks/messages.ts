/** User-facing messages of the task screens (contracts/spa-routes.md). */
export const taskMessages = {
  serviceUnavailable: "We couldn't reach the service. Please try again.",
  created: 'Task created.',
  updated: 'Task updated.',
  deleted: 'Task deleted.',
  restored: 'Task restored.',
  deletedRestoreToEdit: 'This task was deleted. Restore it to edit.',
  deleteConfirmation: 'Delete this task? You can restore it from Deleted tasks.',
} as const

/** Navigation state used to show a one-time message on the next screen. */
export interface TaskStatusState {
  /** A confirmation ("Task created."), announced politely. */
  status?: string | undefined
  /** Something the person must notice ("This task was deleted…"), announced as an alert. */
  alert?: string | undefined
}
