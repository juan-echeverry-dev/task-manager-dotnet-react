import { Link } from 'react-router'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  /** Address of a given page, keeping the rest of the list state. */
  hrefFor: (page: number) => string
}

const linkClass =
  'rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'
const currentClass =
  'rounded-md border border-sky-700 bg-sky-700 px-3 py-1.5 text-sm font-medium text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-600'

/** Page links for a paginated list. Hidden when everything fits on one page. */
export function Pagination({ page, pageSize, totalCount, hrefFor }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize))
  if (pageCount <= 1 && page <= 1) {
    return null
  }

  const pages = visiblePages(Math.min(page, pageCount), pageCount)

  return (
    <nav aria-label="Pagination" className="flex flex-wrap items-center gap-2">
      {page > 1 ? (
        <Link to={hrefFor(Math.min(page - 1, pageCount))} className={linkClass} aria-label="Previous page">
          ‹ Previous
        </Link>
      ) : null}
      {pages.map((number, index) =>
        number === null ? (
          <span key={`gap-${index}`} aria-hidden="true" className="px-1 text-slate-500">
            …
          </span>
        ) : (
          <Link
            key={number}
            to={hrefFor(number)}
            aria-label={`Page ${number}`}
            aria-current={number === page ? 'page' : undefined}
            className={number === page ? currentClass : linkClass}
          >
            {number}
          </Link>
        ),
      )}
      {page < pageCount ? (
        <Link to={hrefFor(page + 1)} className={linkClass} aria-label="Next page">
          Next ›
        </Link>
      ) : null}
    </nav>
  )
}

/** First, last, and the pages around the current one; `null` marks a gap. */
function visiblePages(current: number, pageCount: number): Array<number | null> {
  const numbers = new Set([1, pageCount, current - 1, current, current + 1])
  const sorted = [...numbers].filter((number) => number >= 1 && number <= pageCount).sort((a, b) => a - b)

  const result: Array<number | null> = []
  for (const number of sorted) {
    const previous = result.at(-1)
    if (typeof previous === 'number' && number - previous > 1) result.push(null)
    result.push(number)
  }
  return result
}
