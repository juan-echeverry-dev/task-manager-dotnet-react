import { useId, type ComponentPropsWithRef } from 'react'
import { controlClassName, describedBy } from './field-styles'
import { FieldShell } from './FieldShell'

interface TextFieldProps extends Omit<ComponentPropsWithRef<'input'>, 'id'> {
  label: string
  error?: string | undefined
  /** Smaller variant for toolbars (field-styles.ts). */
  compact?: boolean | undefined
  /** Classes of the field's outer box, for example its width. */
  fieldClassName?: string | undefined
  hint?: string | undefined
}

/**
 * Labelled input whose error message is announced with the field (aria-invalid + aria-describedby).
 */
export function TextField({ label, error, hint, compact, fieldClassName, className, ...inputProps }: TextFieldProps) {
  const id = useId()

  return (
    <FieldShell id={id} label={label} error={error} hint={hint} compact={compact} className={fieldClassName}>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, error, hint)}
        className={controlClassName(error, className, compact)}
        {...inputProps}
      />
    </FieldShell>
  )
}
