import { useId, type ComponentPropsWithRef } from 'react'

interface TextFieldProps extends Omit<ComponentPropsWithRef<'input'>, 'id'> {
  label: string
  error?: string | undefined
}

/**
 * Labelled input whose error message is announced with the field (aria-invalid + aria-describedby).
 */
export function TextField({ label, error, className, ...inputProps }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        className={[
          'block w-full rounded-md border bg-white px-3 py-2 text-base shadow-sm',
          'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600',
          error ? 'border-red-600' : 'border-slate-300',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...inputProps}
      />
      {error ? (
        <p id={errorId} className="text-sm text-red-700">
          {error}
        </p>
      ) : null}
    </div>
  )
}
