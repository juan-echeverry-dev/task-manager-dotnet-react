import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/http-client'
import { SubmitButton } from '../../shared/ui/SubmitButton'
import { TextField } from '../../shared/ui/TextField'
import { register as registerAccount } from './auth-api'
import { authMessages } from './messages'
import { registerSchema, type RegisterValues } from './schemas'
import type { SignInLocationState } from './SignInPage'

const FIELDS = ['displayName', 'email', 'password'] as const

export function RegisterPage() {
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string>()
  const {
    register,
    handleSubmit,
    setError,
    setValue,
    formState: { errors },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '' },
  })
  const mutation = useMutation({ mutationFn: registerAccount })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(undefined)
    try {
      await mutation.mutateAsync(values)
      const state: SignInLocationState = { email: values.email, registered: true }
      await navigate('/sign-in', { state })
    } catch (error) {
      setValue('password', '')
      if (error instanceof ApiError && error.status === 409) {
        setError('email', { message: authMessages.emailAlreadyRegistered }, { shouldFocus: true })
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
      <h1 className="text-2xl font-semibold">Create your account</h1>

      {formError ? (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
          {formError}
        </p>
      ) : null}

      <form noValidate onSubmit={onSubmit} className="space-y-4">
        <TextField label="Display name" autoComplete="name" error={errors.displayName?.message} {...register('displayName')} />
        <TextField label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <TextField
          label="Password"
          type="password"
          autoComplete="new-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <SubmitButton pending={mutation.isPending} label="Create account" pendingLabel="Creating account…" />
      </form>

      <p className="text-sm text-slate-600">
        Already have an account?{' '}
        <Link to="/sign-in" className="font-medium text-sky-700 underline">
          Sign in
        </Link>
      </p>
    </section>
  )
}
