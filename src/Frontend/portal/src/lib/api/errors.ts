/**
 * Typed error surface for API failures.
 *
 * The .NET BFF currently returns tenant resolution failures as JSON shaped like
 * { "error": "tenant_resolution_failed", "reason": "MissingJwtClaim" }. We map
 * those into a structured ApiError so UI can branch on `code` rather than
 * sniffing the message.
 */

export type ApiErrorCode =
  | "tenant_resolution_failed"
  | "unauthorized"
  | "forbidden"
  | "not_found"
  | "validation_failed"
  | "server_error"
  | "network_error"
  | "unknown";

export interface ApiErrorPayload {
  error?: string;
  reason?: string;
  message?: string;
  [key: string]: unknown;
}

export class ApiError extends Error {
  readonly code: ApiErrorCode;
  readonly status: number;
  readonly reason?: string;
  readonly payload?: ApiErrorPayload;

  constructor(args: {
    code: ApiErrorCode;
    status: number;
    message: string;
    reason?: string;
    payload?: ApiErrorPayload;
  }) {
    super(args.message);
    this.name = "ApiError";
    this.code = args.code;
    this.status = args.status;
    this.reason = args.reason;
    this.payload = args.payload;
  }

  /** Friendly fallback for UI — never use status alone in toasts. */
  get displayMessage(): string {
    if (this.code === "tenant_resolution_failed") {
      return `Tenant resolution failed${this.reason ? ` (${this.reason})` : ""}.`;
    }
    if (this.code === "unauthorized") return "You are not signed in.";
    if (this.code === "forbidden") return "You don't have access to this resource.";
    if (this.code === "not_found") return "The requested resource was not found.";
    if (this.code === "network_error") return "Could not reach the server.";
    return this.message;
  }
}

export function mapStatusToCode(status: number, payload?: ApiErrorPayload): ApiErrorCode {
  if (payload?.error === "tenant_resolution_failed") return "tenant_resolution_failed";
  if (status === 401) return "unauthorized";
  if (status === 403) return "forbidden";
  if (status === 404) return "not_found";
  if (status === 422 || status === 400) return "validation_failed";
  if (status >= 500) return "server_error";
  return "unknown";
}
