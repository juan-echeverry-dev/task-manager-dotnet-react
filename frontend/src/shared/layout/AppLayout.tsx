import { Link, Outlet } from 'react-router'
import { useSession } from '../../features/auth/session/useSession'
import { ApiStatus } from '../../features/system-status/ApiStatus'

/** Application shell: header, main content area and footer. */
export function AppLayout() {
  const { session } = useSession()
  const linkClass =
    'rounded-sm font-medium text-sky-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'

  return (
    <div className="flex min-h-dvh flex-col bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3">
          <Link to="/" className="text-lg font-semibold tracking-tight">
            TaskLane
          </Link>
          <nav aria-label="Account" className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
            {session ? (
              <>
                <Link to="/tasks" className={linkClass}>
                  Tasks
                </Link>
                <Link to="/profile" className={linkClass}>
                  Profile
                </Link>
              </>
            ) : (
              <>
                <Link to="/sign-in" className={linkClass}>
                  Sign in
                </Link>
                <Link to="/register" className={linkClass}>
                  Create account
                </Link>
              </>
            )}
            <ApiStatus />
          </nav>
        </div>
      </header>

      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-slate-200 py-4 text-center text-xs text-slate-500">
        TaskLane
      </footer>
    </div>
  )
}
