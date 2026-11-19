import { useId, type ComponentPropsWithRef } from 'react'
import { controlClassName, describedBy } from './field-styles'
import { FieldShell } from './FieldShell'

interface TextFieldProps extends Omit<ComponentPropsWithRef<'input'>, 'id'> {
  label: string
  error?: string | undefined
  hint?: string | undefined
}

/**
 * Labelled input whose error message is announced with the field (aria-invalid + aria-describedby).
 */
export function TextField({ label, error, hint, className, ...inputProps }: TextFieldProps) {
  const id = useId()

  return (
    <FieldShell id={id} label={label} error={error} hint={hint}>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, error, hint)}
        className={controlClassName(error, className)}
        {...inputProps}
      />
    </FieldShell>
  )
}
