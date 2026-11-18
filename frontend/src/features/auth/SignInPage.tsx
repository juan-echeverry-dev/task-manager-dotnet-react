import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { SubmitButton } from '../../shared/ui/SubmitButton'
import { TextField } from '../../shared/ui/TextField'
import { signIn as requestSignIn } from './auth-api'
import { authMessages } from './messages'
import { signInSchema, type SignInValues } from './schemas'
import { useSession } from './session/useSession'

/** Navigation state accepted by the sign-in screen. */
export interface SignInLocationState {
  email?: string | undefined
  registered?: boolean | undefined
  reason?: 'expired' | undefined
}

const FIELDS = ['email', 'password'] as const

export function SignInPage() {
  const state = (useLocation().state ?? {}) as SignInLocationState
  const { signIn, acknowledgeEnd } = useSession()
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    setValue,
    formState: { errors },
  } = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { email: state.email ?? '', password: '' },
  })
  const mutation = useMutation({ mutationFn: requestSignIn })

  // The reason a previous session ended is shown once, from the navigation state.
  useEffect(() => acknowledgeEnd(), [acknowledgeEnd])

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      const result = await mutation.mutateAsync(values)
      // RedirectIfAuthenticated then sends the person to the safe return destination.
      signIn({ accessToken: result.accessToken, expiresAt: result.expiresAt })
    } catch (error) {
      setValue('password', '')
      if (error instanceof ApiError && error.status === 401) {
        setFormError(authMessages.invalidCredentials)
      } else if (error instanceof ApiError && error.status === 400 && error.problem?.errors) {
        for (const field of FIELDS) {
          const message = error.problem.errors[field]?.[0]
          if (message) setError(field, { message })
        }
      } else {
        setFormError(authMessages.serviceUnavailable)
      }
    }
  })

  return (
    <section className="mx-auto w-full max-w-sm space-y-6">
      <h1 className="text-2xl font-semibold">Sign in</h1>

      {state.registered && !formError ? (
        <p role="status" className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
          {authMessages.accountCreated}
        </p>
      ) : null}

      {state.reason === 'expired' && !formError ? (
        <p role="alert" className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
          {authMessages.sessionExpired}
        </p>
      ) : null}

      {formError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {formError}
        </p>
      ) : null}

      <form noValidate onSubmit={onSubmit} className="space-y-4">
        <TextField label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <SubmitButton pending={mutation.isPending} label="Sign in" pendingLabel="Signing in…" />
      </form>

      <p className="text-sm text-slate-600">
        New here?{' '}
        <Link to="/register" className="font-medium text-sky-700 underline">
          Create an account
        </Link>
      </p>
    </section>
  )
}
