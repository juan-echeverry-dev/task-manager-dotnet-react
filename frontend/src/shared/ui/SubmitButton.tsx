interface SubmitButtonProps {
  pending: boolean
  label: string
  pendingLabel: string
}

/** Submit button that is disabled and announces progress while a request is in flight (FR-018). */
export function SubmitButton({ pending, label, pendingLabel }: SubmitButtonProps) {
  return (
    <button
      type="submit"
      disabled={pending}
      aria-busy={pending}
      className="w-full rounded-md bg-sky-700 px-4 py-2 font-medium text-white shadow-sm hover:bg-sky-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600 disabled:cursor-not-allowed disabled:opacity-60"
    >
      {pending ? pendingLabel : label}
    </button>
  )
}
