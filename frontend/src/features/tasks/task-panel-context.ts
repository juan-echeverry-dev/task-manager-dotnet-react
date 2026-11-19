import { useOutletContext } from 'react-router'

/** What the side panel shell passes to its content (details, create, edit). */
export interface TaskPanelContext {
  /** Id of the content heading that names the panel. */
  titleId: string
  /** Board query string, to keep on every board address. */
  search: string
}

export function useTaskPanel() {
  return useOutletContext<TaskPanelContext>()
}
