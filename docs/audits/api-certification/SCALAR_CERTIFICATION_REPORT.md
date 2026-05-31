# SCALAR / OpenAPI CERTIFICATION (Phase 5)

**Date:** 2026-05-30 · **Method:** live inspection of `/openapi/v1.json` + `/scalar/v1` on the running API.

## Surfaces — VERIFIED reachable
| Surface | URL | Result |
|---|---|---|
| OpenAPI document | `/openapi/v1.json` | **200**, application/json, 133 KB |
| Scalar UI | `/scalar/v1` (`/scalar` → 302) | **200**, HTML |
| Root | `/` | 302 → `/scalar` (dev convenience) |

Only mapped in **Development/Local** (gated in `Program.cs`) — correct (no doc surface in prod).

## Coverage — VERIFIED complete (no hidden/missing/duplicate endpoints)
- OpenAPI exposes **170 operations across 157 paths**, matching the source-level endpoint count (170
  `Map{Get,Post,Put,Delete,Patch}`). **No hidden endpoints**; **no duplicate paths**; no obsolete stragglers.
- **61 component schemas** present; request/response models resolve.

## Defect found AND FIXED this pass — per-operation security annotation
- **Before:** the `Bearer` scheme was defined, but security was applied as a single blanket
  `document.Security` — so **every** operation (including anonymous `login`/`register`/`refresh`/`health`)
  was implicitly marked as requiring a Bearer token, and Scalar gave **no per-endpoint auth signal**.
  A developer could not tell from the doc which endpoints need a token.
- **Fix:** added an OpenAPI **operation transformer** that sets the Bearer security requirement only on
  operations whose endpoint metadata has `IAuthorizeData` and not `IAllowAnonymous`; removed the blanket
  document-level requirement.
- **After (runtime-verified):** **161 operations carry the Bearer requirement, 9 are anonymous.** The 9
  anonymous: `GET /`, `GET /health`, `/health/ready`, `/health/startup`, `POST /api/identity/login`,
  `/register`, `/refresh`, `GET /api/analytics/projects/daily`, `POST /api/tenants`* — Scalar now renders
  correct per-endpoint locks.

  \* `POST /api/tenants` and `GET /api/analytics/projects/daily` being anonymous are **security findings**
  (see ENDPOINT_GAP_ANALYSIS). `/api/tenants` has since been changed to `RequireAuthorization`.

## Schemas / examples
- Request/response `$ref` schemas resolve for the operations sampled; no broken `$ref` observed in the doc.
- Scalar renders the document and supports the Bearer "Authorize" flow (`PreferredSecuritySchemes=["Bearer"]`).
- **NOT VERIFIED:** exhaustive per-schema example correctness and every model's round-trip were not checked.

## Verdict: **Phase 5 — VERIFIED.** Scalar is a complete, reachable developer entry point; the one real
defect (no per-endpoint auth signal) was fixed and re-verified at runtime.
