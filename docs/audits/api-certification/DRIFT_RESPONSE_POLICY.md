# DRIFT RESPONSE POLICY

**Date:** 2026-05-30
**Status:** ADOPTED

## Purpose

This policy defines how the Karamchari platform responds when a tenant schema drifts from the EF Core source model. The platform must never fail globally because a single tenant experiences schema drift.

---

## Environment-Specific Behaviour

### Local (Development)

| Trigger | Drift detected at startup |
|---|---|
| **Behaviour** | **Fail Startup** |
| **Surface** | Exception thrown in `IHostedService`; process exits |
| **Purpose** | Immediate developer feedback — the problem is visible before any request is served |

Implementation note: `TenantProvisioningService` performs Artifact Set Verification (ASV) on every `--provision-dev-tenants` run. Any artifact count or schema mismatch causes a `TenantProvisioningException`.

---

### CI

| Trigger | Drift detected during pipeline run |
|---|---|
| **Behaviour** | **Fail Build** |
| **Surface** | Non-zero exit code; failing test in `Karamchari.TenantIsolationCertification` or provisioning integration test |
| **Purpose** | Prevent drift from reaching shared environments |

Implementation note: The CI integration test suite executes `--provision-dev-tenants` against a clean SQL Server container and asserts that all tenants pass ASV. A failure blocks merge.

---

### Staging

| Trigger | Tenant schema fails ASV during startup reconciliation |
|---|---|
| **Behaviour** | **Quarantine Tenant + Generate Alert** |
| **Surface** | Tenant marked `Quarantined` in platform registry; alert published to ops channel |
| **Purpose** | Prevent cross-tenant impact while preserving the staging environment for other tenants |

Actions:
1. Mark the affected tenant as `Quarantined` in the tenant registry.
2. Return `503 Service Unavailable` for all API calls scoped to that tenant.
3. Publish a `TenantSchemaAlertEvent` to the operations notification channel.
4. Preserve all tenant data — no schema mutations without explicit operator action.

---

### Production

| Trigger | Tenant schema fails ASV during startup reconciliation |
|---|---|
| **Behaviour** | **Quarantine Tenant + Generate Alert + Preserve Other Tenants** |
| **Surface** | Tenant `Quarantined`; alert dispatched; all other tenants continue to operate normally |
| **Purpose** | Prevent platform-wide outage; a single bad tenant must never cascade |

Actions:
1. Mark the affected tenant as `Quarantined`.
2. Return `503 Service Unavailable` for that tenant's API surface.
3. Dispatch a `P1 TenantSchemaDriftAlert` (PagerDuty / ops channel).
4. Log the specific missing artifacts for operator review.
5. Allow the operator to trigger `additive migration` (see TENANT_MIGRATION_STRATEGY.md).
6. Never drop, recreate, or destructively alter any tenant schema.

---

## Quarantine Contract

A quarantined tenant:
- Returns HTTP `503` for all data-plane requests
- Returns HTTP `200` for the platform health check (so load balancers don't remove healthy pods)
- Remains quarantined until an operator explicitly triggers reconciliation and ASV re-certifies the schema

---

## Drift Severity Classification

| Severity | Description | Example |
|---|---|---|
| **P1 – Critical** | Missing table referenced by active application code | Missing `tenant_x.Workflow_StepInstances` |
| **P2 – Degraded** | Missing column or index; feature partially broken | Missing nullable column on existing table |
| **P3 – Warning** | Extra artifacts not in source model (forward-compatibility) | Extra column added by a side channel |

P1 and P2 trigger quarantine in Staging/Production. P3 is logged as a warning but does not quarantine.

---

## Evidence of Compliance

- Startup ASV implemented in `TenantProvisioningService.VerifyArtifactEqualityAsync`.
- All four dev tenants certified as `ZERO_DRIFT` per `ARTIFACT_EQUALITY_REPORT.md`.
- Quarantine logic is the responsibility of the `TenantHealthGuard` service (scheduled for implementation in Platform Ops sprint).
