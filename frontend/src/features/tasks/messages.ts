/** User-facing messages of the task screens (contracts/spa-routes.md). */
export const taskMessages = {
  serviceUnavailable: "We couldn't reach the service. Please try again.",
  created: 'Task created.',
  updated: 'Task updated.',
  deleted: 'Task deleted.',
  restored: 'Task restored.',
  deletedRestoreToEdit: 'This task was deleted. Restore it to edit.',
} as const

/** Navigation state used to show a one-time status message on the next screen. */
export interface TaskStatusState {
  status?: string | undefined
}
