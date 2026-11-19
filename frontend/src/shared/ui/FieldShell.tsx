import type { ReactNode } from 'react'

interface FieldShellProps {
  id: string
  label: string
  error: string | undefined
  hint?: string | undefined
  /** Smaller label and messages, for toolbars. */
  compact?: boolean | undefined
  /** Classes of the field's outer box, for example its width in a row of fields. */
  className?: string | undefined
  children: ReactNode
}

/** Label, control and error message laid out the same way for every form control. */
export function FieldShell({ id, label, error, hint, compact, className, children }: FieldShellProps) {
  const text = compact ? 'text-xs' : 'text-sm'
  return (
    <div className={['space-y-1', className].filter(Boolean).join(' ')}>
      <label
        htmlFor={id}
        className={
          compact
            ? 'block text-xs font-medium tracking-wide text-slate-500 uppercase'
            : 'block text-sm font-medium text-slate-700'
        }
      >
        {label}
      </label>
      {children}
      {hint ? (
        <p id={`${id}-hint`} className={`${text} text-slate-500`}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={`${id}-error`} className={`${text} text-red-700`}>
          {error}
        </p>
      ) : null}
    </div>
  )
}
