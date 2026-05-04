import { apiConfig } from "./config";
import { ApiError, mapStatusToCode, type ApiErrorPayload } from "./errors";

/**
 * Fetch-based API client.
 *
 * Auto-attaches:
 *   - X-Tenant-Id            tenant identifier (default from NEXT_PUBLIC_DEFAULT_TENANT_ID)
 *   - X-Karamchari-Gateway   dev gateway-proof fingerprint (NEXT_PUBLIC_GATEWAY_FINGERPRINT)
 *   - Accept                 application/json
 *   - Content-Type           application/json (only when a body is present)
 *
 * Why both headers? The .NET HttpTenantProvider only honours X-Tenant-Id when
 * the trusted gateway proof header is also present and matches the configured
 * fingerprint — see Karamchari.Core/Multitenancy/HttpTenantProvider.cs and
 * Karamchari.Api/Security/DevelopmentTenantAuthenticationHandler.cs.
 *
 * We use fetch (not axios) to keep the dependency footprint small and to lean on
 * Next.js's native request deduping when this client is used in server components.
 */

export interface ApiRequestInit extends Omit<RequestInit, "body" | "headers"> {
  /** JSON body — serialised automatically. Use `rawBody` for non-JSON. */
  body?: unknown;
  /** Pre-serialised body (FormData, ArrayBuffer, etc.). */
  rawBody?: BodyInit;
  /** Override the tenant id for this single call (e.g. cross-tenant admin views). */
  tenantId?: string;
  /** Per-request header overrides. Tenant + gateway headers always win unless explicitly null. */
  headers?: Record<string, string>;
}

const TENANT_HEADER = "X-Tenant-Id";
const GATEWAY_HEADER = "X-Karamchari-Gateway";

async function request<T>(path: string, init: ApiRequestInit = {}): Promise<T> {
  const url = buildUrl(path);
  const tenantId = init.tenantId ?? apiConfig.defaultTenantId;

  const headers: Record<string, string> = {
    Accept: "application/json",
    [TENANT_HEADER]: tenantId,
    [GATEWAY_HEADER]: apiConfig.gatewayFingerprint,
    ...(init.headers ?? {}),
  };

  let body: BodyInit | undefined;
  if (init.rawBody !== undefined) {
    body = init.rawBody;
  } else if (init.body !== undefined) {
    body = JSON.stringify(init.body);
    headers["Content-Type"] = headers["Content-Type"] ?? "application/json";
  }

  let response: Response;
  try {
    response = await fetch(url, {
      ...init,
      headers,
      body,
      // Important: do NOT send cookies; we're auth'd via headers in dev and
      // bearer tokens in prod. Sending cookies would break CORS preflights.
      credentials: "omit",
    });
  } catch (cause) {
    throw new ApiError({
      code: "network_error",
      status: 0,
      message: `Network error reaching ${url}`,
      payload: { message: cause instanceof Error ? cause.message : String(cause) },
    });
  }

  if (response.status === 204 || response.status === 205) {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.includes("application/json");

  if (!response.ok) {
    let payload: ApiErrorPayload | undefined;
    if (isJson) {
      payload = (await response.json().catch(() => undefined)) as ApiErrorPayload | undefined;
    } else {
      const text = await response.text().catch(() => "");
      payload = text ? { message: text } : undefined;
    }
    throw new ApiError({
      code: mapStatusToCode(response.status, payload),
      status: response.status,
      message: payload?.message ?? `Request failed: ${response.status} ${response.statusText}`,
      reason: payload?.reason,
      payload,
    });
  }

  if (!isJson) {
    // Non-JSON 2xx is unusual for our API; surface it as text.
    return (await response.text()) as unknown as T;
  }
  return (await response.json()) as T;
}

function buildUrl(path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }
  const base = apiConfig.baseUrl.replace(/\/$/, "");
  const suffix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${suffix}`;
}

export const api = {
  get<T>(path: string, init?: ApiRequestInit): Promise<T> {
    return request<T>(path, { ...init, method: "GET" });
  },
  post<T>(path: string, init?: ApiRequestInit): Promise<T> {
    return request<T>(path, { ...init, method: "POST" });
  },
  put<T>(path: string, init?: ApiRequestInit): Promise<T> {
    return request<T>(path, { ...init, method: "PUT" });
  },
  patch<T>(path: string, init?: ApiRequestInit): Promise<T> {
    return request<T>(path, { ...init, method: "PATCH" });
  },
  delete<T>(path: string, init?: ApiRequestInit): Promise<T> {
    return request<T>(path, { ...init, method: "DELETE" });
  },
};
