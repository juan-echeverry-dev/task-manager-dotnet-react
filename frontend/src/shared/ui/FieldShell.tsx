import type { ReactNode } from 'react'

interface FieldShellProps {
  id: string
  label: string
  error: string | undefined
  hint?: string | undefined
  children: ReactNode
}

/** Label, control and error message laid out the same way for every form control. */
export function FieldShell({ id, label, error, hint, children }: FieldShellProps) {
  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>
      {children}
      {hint ? (
        <p id={`${id}-hint`} className="text-sm text-slate-500">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={`${id}-error`} className="text-sm text-red-700">
          {error}
        </p>
      ) : null}
    </div>
  )
}
