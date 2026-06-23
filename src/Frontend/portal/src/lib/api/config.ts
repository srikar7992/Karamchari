/**
 * Centralised env-var reads for the API client.
 *
 * `NEXT_PUBLIC_*` values are inlined into the browser bundle at build time, so
 * everything here ships to the client. That's fine for non-secrets (API base URL,
 * default tenant id, dev gateway fingerprint) — and we explicitly do NOT put any
 * secret here.
 *
 * In production:
 *   - Tenant id comes from the user's signed JWT, not a frontend default.
 *   - The gateway-proof header is injected by APIM, not by the SPA.
 *
 * The defaults below exist purely so local dev works against
 * appsettings.Development.json without further config.
 */

export const apiConfig = Object.freeze({
  baseUrl: requireEnv(
    "NEXT_PUBLIC_API_BASE_URL",
    process.env.NEXT_PUBLIC_API_BASE_URL,
    "http://localhost:60463",
  ),
  defaultTenantId: requireEnv(
    "NEXT_PUBLIC_DEFAULT_TENANT_ID",
    process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID,
    "sch_oakridge",
  ),
  gatewayFingerprint: requireEnv(
    "NEXT_PUBLIC_GATEWAY_FINGERPRINT",
    process.env.NEXT_PUBLIC_GATEWAY_FINGERPRINT,
    "local-dev-gateway",
  ),
} as const);

function requireEnv(name: string, value: string | undefined, fallback: string): string {
  if (value && value.trim().length > 0) {
    return value.trim();
  }
  if (process.env.NODE_ENV !== "production") {
    return fallback;
  }
  throw new Error(`Required env var ${name} is not set`);
}
