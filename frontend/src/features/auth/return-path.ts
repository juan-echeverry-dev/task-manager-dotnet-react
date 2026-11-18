import { matchRoutes } from 'react-router'
import { routes } from '../../app/routes'

export const DEFAULT_SIGNED_IN_PATH = '/profile'

/**
 * Where to go after signing in. Only an existing internal screen is accepted; anything else
 * (external, protocol-relative, malformed, or unknown) falls back to the profile, so a crafted
 * link cannot redirect people off the application (spec FR-012, Clarification Q4).
 */
export function safeReturnPath(value: string | null): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return DEFAULT_SIGNED_IN_PATH
  }

  let url: URL
  try {
    url = new URL(value, window.location.origin)
  } catch {
    return DEFAULT_SIGNED_IN_PATH
  }

  if (url.origin !== window.location.origin) {
    return DEFAULT_SIGNED_IN_PATH
  }

  const matches = matchRoutes(routes, url.pathname)
  if (!matches || matches.some((match) => match.route.path === '*')) {
    return DEFAULT_SIGNED_IN_PATH
  }

  return `${url.pathname}${url.search}${url.hash}`
}
