// Phase 0 — the live seam. Single HTTP client for the Karamchari BFF.
// Every page that consumes real data goes through here: base URL, Bearer
// attachment, JSON parsing, and a typed error. No fetch() calls scattered
// across pages — one client, one contract surface.

const API_BASE =
  (import.meta.env.VITE_API_BASE as string | undefined) ?? 'http://localhost:60463'

export class ApiError extends Error {
  readonly status: number
  readonly statusText: string
  readonly body: unknown

  constructor(status: number, statusText: string, body: unknown) {
    super(`API ${status} ${statusText}`)
    this.name = 'ApiError'
    this.status = status
    this.statusText = statusText
    this.body = body
  }
}

let accessToken: string | null = localStorage.getItem('karam-access-token')

export function setAccessToken(token: string | null) {
  accessToken = token
  if (token) localStorage.setItem('karam-access-token', token)
  else localStorage.removeItem('karam-access-token')
}

export function getAccessToken(): string | null {
  return accessToken
}

// Re-auth hook. auth.ts registers a handler that re-logs-in the dev operator.
// Lets the client transparently recover from an expired token (e.g. a stale
// localStorage token after the dev server was idle) instead of surfacing a 401.
let reauthHandler: (() => Promise<void>) | null = null
export function setReauthHandler(fn: (() => Promise<void>) | null) {
  reauthHandler = fn
}

interface RequestOptions {
  method?: string
  body?: unknown
  /** When false, the Authorization header is not attached (for login/register). */
  auth?: boolean
  /** Internal: set once a 401 retry has already been attempted, to avoid loops. */
  retried?: boolean
}

/** Core request. Returns parsed JSON of type T, or throws ApiError. */
export async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true } = opts
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (auth && accessToken) headers.Authorization = `Bearer ${accessToken}`

  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  // Transparent recovery from an expired/stale token: re-authenticate once and retry.
  if (res.status === 401 && auth && !opts.retried && reauthHandler) {
    await reauthHandler()
    return request<T>(path, { ...opts, retried: true })
  }

  const text = await res.text()
  const parsed = text ? safeJson(text) : null

  if (!res.ok) throw new ApiError(res.status, res.statusText, parsed ?? text)
  return parsed as T
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown, auth = true) =>
    request<T>(path, { method: 'POST', body, auth }),
  base: API_BASE,
}
