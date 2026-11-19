import { useInfiniteQuery } from '@tanstack/react-query'
import { useSession } from '../auth/session/useSession'
import { columnParams, type BoardQuery } from './board-query'
import { listTasks, type Task, type TaskStatus } from './tasks-api'

export interface BoardColumnData {
  tasks: Task[]
  /** Matching tasks in this status; undefined until the first batch loads. */
  totalCount: number | undefined
  hasMore: boolean
  loadMore: () => void
  isLoading: boolean
  isLoadingMore: boolean
  error: unknown
}

/**
 * One board column: the person's tasks in `status` that match the board query, loaded 20 at a time
 * (spec 004 FR-006, research R2). Mutations invalidate `['tasks']`, which refreshes every column.
 */
export function useBoardColumn(status: TaskStatus, query: BoardQuery): BoardColumnData {
  const accessToken = useSession().session?.accessToken ?? ''
  const params = columnParams(query, status)

  const column = useInfiniteQuery({
    queryKey: ['tasks', 'board', status, params, accessToken],
    queryFn: ({ pageParam }) =>
      listTasks(pageParam === 1 ? params : { ...params, page: String(pageParam) }, accessToken),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.page * last.pageSize < last.totalCount ? last.page + 1 : undefined),
    enabled: accessToken !== '',
  })

  return {
    tasks: column.data?.pages.flatMap((page) => page.items) ?? [],
    totalCount: column.data?.pages[0]?.totalCount,
    hasMore: column.hasNextPage,
    loadMore: () => void column.fetchNextPage(),
    isLoading: column.isPending,
    isLoadingMore: column.isFetchingNextPage,
    error: column.error,
  }
}
