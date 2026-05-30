# ENDPOINT / CAPABILITY GAP ANALYSIS (Phase 9)

**Date:** 2026-05-30 · Hostile review of runtime behaviour. Findings ordered by severity.

## 🔴 GAP-1 (HIGH) — Tenant schemas are missing ~24% of their tables (provisioning incomplete)
- **Evidence:** `dbo` (template) = **181 tables**; each provisioned tenant schema (`tenant_dev/acme/globex`)
  = **137 tables**. **~44 tables per tenant are never created.** Confirmed missing examples:
  `LeaveBalanceEntries`, `Workflow_StepInstances` exist in `dbo` but in **no** `tenant_*` schema.
- **Runtime impact:** any endpoint touching a missing table throws `Invalid object name 'tenant_X.Table'` →
  **HTTP 500**. Confirmed: `GET /api/v1/time/leave-balances` (→ `LeaveBalanceEntries`) and
  `GET /api/v1/approvals/my` (→ `Workflow_StepInstances`) both 500. Likely affects more endpoints.
- **Root cause:** `TenantProvisioningService` clones only tables in `ITenantTableRegistry` (137), which is
  **hand-populated** by each module via `Register(TenantTable)`. Modules registered parent tables but **not
  their EF owned-collection child tables** (e.g. `LeaveBalances` registered, `LeaveBalanceEntries` not).
- **Business justification:** without these tables, leave management, workflow approvals, and any module with
  owned-collection persistence are non-functional per-tenant. Blocks module certification.
- **Suggested remediation (priority P0):** derive the tenant table set from the EF model's tenant-scoped
  entity types (including owned-collection tables) instead of a hand-maintained registry; OR add the missing
  ~44 tables to the registry. Then re-provision. **Owner:** Platform/Core (provisioning) + each module owner.

## 🟠 GAP-2 (HIGH) — Permission-gated endpoints unreachable (login emits empty permissions)
- **Evidence:** `POST /api/identity/login` calls `GenerateAccessTokenAsync(..., roles, permissions: [])` —
  the **permission list is always empty**. `GET /api/v1/capability/skills` (requires `capability.read`)
  returns **403 even for `admin@dev.local`** (`User ... denied permission capability.read`).
- **Impact:** every endpoint gated by a fine-grained permission is unreachable by any user. Roles are issued
  but never mapped to permissions.
- **Suggested remediation (P1):** wire a role→permission mapping at login (or persist permission claims per
  user/role) and include them in the JWT. **Owner:** Identity.

## 🟠 GAP-3 (MED→HIGH) — Anonymous privileged endpoints
- `POST /api/tenants` (tenant provisioning — creates schemas, publishes events) was **anonymous**.
  **Fixed this pass** → now `RequireAuthorization()` (verified: no-auth 401, auth 201).
- `GET /api/analytics/projects/daily` is **anonymous** — verify it exposes no tenant data; add auth if it does.
  **Owner:** Platform/PSA.

## 🟡 GAP-4 (MED) — Developer/reference/search/bulk capabilities thin
Hostile "what would a new engineer expect and not find":
- **Reference/lookup APIs:** no obvious read-only enums/lookup endpoints (statuses, roles, leave types,
  departments list) — developers must infer valid values from code/DTOs.
- **Search:** only a narrow `SearchEndpoints` (3 ops); no generic per-module list/filter/paginate contract.
- **Bulk APIs:** no bulk create/update/import endpoints (e.g. bulk employee import) — onboarding at scale
  requires N calls.
- **Export APIs:** `ExportJobEndpoints` exist for HR reports; no general CSV/Excel export across modules.
- **Admin APIs:** no platform-admin surface for user/role management beyond register/login (Identity has no
  list-users / assign-role / deactivate endpoints exposed).
- **Suggested remediation (P2):** define a standard list/search/paginate contract; add reference-data
  endpoints per module; add bulk import + export where business-relevant.

## 🟢 GAP-5 (LOW) — `GET /api/v1/billing/ar/summary` returned 404 in cross-section probe
- Likely path/group nuance or requires seeded billing contracts; not fully characterized. **Owner:** Billing.

## What is NOT a gap (verified working)
- Discovery (Scalar/OpenAPI) complete; default credentials for 4 tenants × 4 roles; auth/JWT/refresh;
  tenant isolation (incl. new `globex`); employee onboarding → async payroll profile; health; observability.
