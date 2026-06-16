// Phase 0 — real JWT login against /api/identity/login. Exercises the actual
// authentication path (not gateway-header trust) so login latency is measurable.
// Dev convenience: ensureDevSession() logs in as the documented seeded operator
// when no token is held. Credentials come from Vite env vars so no secret is
// committed; set them in karamchari-web/.env.local (gitignored). See
// .env.example for the documented dev defaults (DevDataSeeder.cs /
// AUTHENTICATION_GUIDE.md). Dev only.

import { api, setAccessToken, getAccessToken, setReauthHandler } from './api'

interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
}

// Documented seeded developer operator (tenant 'dev', role Manager). The email
// is a public default; the password must be provided via VITE_DEV_PASSWORD.
const DEV_EMAIL = (import.meta.env.VITE_DEV_EMAIL as string | undefined) ?? 'manager@dev.local'
const DEV_PASSWORD = import.meta.env.VITE_DEV_PASSWORD as string | undefined

let lastLoginMs: number | null = null
export function getLastLoginMs(): number | null {
  return lastLoginMs
}

export async function login(email: string, password: string): Promise<void> {
  const t0 = performance.now()
  const res = await api.post<LoginResponse>(
    '/api/identity/login',
    { email, password },
    /* auth */ false,
  )
  lastLoginMs = Math.round(performance.now() - t0)
  setAccessToken(res.accessToken)
}

/** Ensures a session exists; logs in with seeded dev creds if none. Returns true if a login round-trip happened. */
export async function ensureDevSession(): Promise<boolean> {
  if (getAccessToken()) return false
  if (!DEV_PASSWORD) {
    console.warn('ensureDevSession: VITE_DEV_PASSWORD is not set. Add it to karamchari-web/.env.local to enable dev auto-login.')
    return false
  }
  await login(DEV_EMAIL, DEV_PASSWORD)
  return true
}

export function logout(): void {
  setAccessToken(null)
}

// On any 401, the API client calls this to recover: drop the stale token and
// log the dev operator back in, then the original request is retried once.
setReauthHandler(async () => {
  setAccessToken(null)
  if (!DEV_PASSWORD) return
  await login(DEV_EMAIL, DEV_PASSWORD)
})
