/** RFC 9457 Problem Details, as returned by the API for every error response. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

/** Error thrown for any non-2xx API response. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | undefined

  constructor(status: number, problem: ProblemDetails | undefined) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export interface RequestOptions {
  /** Access token sent as a bearer credential for protected endpoints. */
  accessToken?: string | undefined
}

/**
 * Thin wrapper over fetch: sends/receives JSON and turns error responses into {@link ApiError}.
 */
export async function request<T>(path: string, init: RequestInit = {}, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (options.accessToken) {
    headers.set('Authorization', `Bearer ${options.accessToken}`)
  }

  const response = await fetch(path, { ...init, headers })

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (isJson(response) ? await response.json() : await response.text()) as T
}

function isJson(response: Response): boolean {
  return (response.headers.get('content-type') ?? '').includes('json')
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  if (!isJson(response)) {
    return undefined
  }

  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return undefined
  }
}
