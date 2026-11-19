import { useId, type ComponentPropsWithRef } from 'react'
import { controlClassName, describedBy } from './field-styles'
import { FieldShell } from './FieldShell'

interface TextAreaFieldProps extends Omit<ComponentPropsWithRef<'textarea'>, 'id'> {
  label: string
  error?: string | undefined
}

/** Labelled text area whose error message is announced with the field (aria-invalid + aria-describedby). */
export function TextAreaField({ label, error, className, rows = 4, ...textAreaProps }: TextAreaFieldProps) {
  const id = useId()

  return (
    <FieldShell id={id} label={label} error={error}>
      <textarea
        id={id}
        rows={rows}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, error)}
        className={controlClassName(error, className)}
        {...textAreaProps}
      />
    </FieldShell>
  )
}
