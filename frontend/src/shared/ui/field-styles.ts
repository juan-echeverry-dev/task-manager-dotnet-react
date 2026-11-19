/**
 * Classes shared by the form controls so inputs, selects and text areas look and focus the same.
 * `compact` is the smaller variant used in toolbars such as the task board's filter bar.
 */
export function controlClassName(error: string | undefined, className?: string, compact = false): string {
  return [
    'block w-full rounded-md border bg-white shadow-sm',
    compact ? 'px-2.5 py-1.5 text-sm' : 'px-3 py-2 text-base',
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600',
    error ? 'border-red-600' : 'border-slate-300',
    className,
  ]
    .filter(Boolean)
    .join(' ')
}

/** The ids that describe a control: its hint and, when present, its error. */
export function describedBy(id: string, error: string | undefined, hint?: string | undefined): string | undefined {
  const ids = [hint ? `${id}-hint` : undefined, error ? `${id}-error` : undefined].filter(Boolean)
  return ids.length > 0 ? ids.join(' ') : undefined
}
