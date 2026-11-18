import { useLocation } from 'react-router'
import { TextField } from '../../shared/ui/TextField'
import { authMessages } from './messages'

/** Navigation state accepted by the sign-in screen. */
export interface SignInLocationState {
  email?: string
  registered?: boolean
}

/** Sign-in screen. Only the hand-off from registration exists yet; sign-in itself comes with US2. */
export function SignInPage() {
  const state = (useLocation().state ?? {}) as SignInLocationState

  return (
    <section className="mx-auto w-full max-w-sm space-y-6">
      <h1 className="text-2xl font-semibold">Sign in</h1>

      {state.registered ? (
        <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
          {authMessages.accountCreated}
        </p>
      ) : null}

      <form noValidate className="space-y-4">
        <TextField label="Email" type="email" autoComplete="email" defaultValue={state.email ?? ''} />
      </form>
    </section>
  )
}
