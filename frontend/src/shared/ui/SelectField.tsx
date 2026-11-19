import { useId, type ComponentPropsWithRef } from 'react'
import { controlClassName, describedBy } from './field-styles'
import { FieldShell } from './FieldShell'

interface SelectFieldProps extends Omit<ComponentPropsWithRef<'select'>, 'id'> {
  label: string
  options: ReadonlyArray<{ value: string; label: string }>
  error?: string | undefined
  /** Smaller variant for toolbars (field-styles.ts). */
  compact?: boolean | undefined
  /** Classes of the field's outer box, for example its width. */
  fieldClassName?: string | undefined
}

/** Labelled select whose error message is announced with the field (aria-invalid + aria-describedby). */
export function SelectField({ label, options, error, compact, fieldClassName, className, ...selectProps }: SelectFieldProps) {
  const id = useId()

  return (
    <FieldShell id={id} label={label} error={error} compact={compact} className={fieldClassName}>
      <select
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(id, error)}
        className={controlClassName(error, className, compact)}
        {...selectProps}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FieldShell>
  )
}
