import { Link, Outlet } from 'react-router'
import { ApiStatus } from '../../features/system-status/ApiStatus'

/** Application shell: header, main content area and footer. */
export function AppLayout() {
  return (
    <div className="flex min-h-dvh flex-col bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 px-4 py-3">
          <Link to="/" className="text-lg font-semibold tracking-tight">
            TaskLane
          </Link>
          <ApiStatus />
        </div>
      </header>

      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-slate-200 py-4 text-center text-xs text-slate-500">
        TaskLane
      </footer>
    </div>
  )
}
