import { Link } from 'react-router'

/** Same screen for a missing task and another person's task (spec FR-002). */
export function TaskNotFound() {
  return (
    <section className="mx-auto w-full max-w-lg space-y-3 text-center">
      <h1 className="text-2xl font-semibold">Task not found</h1>
      <p className="text-slate-600">The task does not exist.</p>
      <Link to="/tasks" className="font-medium text-sky-700 underline">
        Back to tasks
      </Link>
    </section>
  )
}
