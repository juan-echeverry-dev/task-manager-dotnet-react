import { Link } from 'react-router'

/**
 * Same panel content for a missing task and another person's task (spec 002 FR-002). The heading names
 * the side panel through `titleId`.
 */
export function TaskNotFound({ titleId }: { titleId?: string | undefined }) {
  return (
    <section className="space-y-3 text-center">
      <h2 id={titleId} className="text-xl font-semibold">
        Task not found
      </h2>
      <p className="text-slate-600">The task does not exist.</p>
      <Link to="/tasks" replace className="font-medium text-sky-700 underline">
        Back to tasks
      </Link>
    </section>
  )
}
