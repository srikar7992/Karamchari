# DRIFT RESPONSE POLICY

**Date:** 2026-05-30
**Scope:** Platform-wide Schema Drift Governance

## 1. Overview
The platform must maintain architectural integrity without sacrificing global availability. This policy defines environment-specific responses to detected schema drift (mismatch between the EF Core model and physical tenant schemas).

## 2. Drift Severity Matrix

| Environment | Behavior | Purpose |
|---|---|---|
| **Local** | **Fail Startup** | Provides immediate feedback to developers when an entity or owned collection is added without proper provisioning coverage. |
| **CI** | **Fail Build** | Prevents drift from reaching shared environments or being merged into the main branch. |
| **Staging** | **Quarantine Tenant** | Blocks access to the drifted tenant (HTTP 503) and generates a P1 alert. Healthy tenants are unaffected. |
| **Production** | **Quarantine Tenant** | Prevents data corruption and silent application logic failures. Generates P1 alert for manual intervention or additive migration. |

## 3. Quarantining Mechanism
When the `TenantProvisioningService` (or a background health checker) identifies an artifact mismatch for a specific tenant:
1. The tenant is flagged as `Quarantined` in the platform registry.
2. The `TenantAuthorizationMiddleware` intercepts all requests for that tenant and returns `503 Service Unavailable (Tenant Schema Drift)`.
3. The platform continues to serve traffic for all non-quarantined tenants.

## 4. Remediation Path
- **Local/CI:** Developers must ensure all bounded-context DbContexts are registered and identifiable by the `ITenantModelDiscoveryService`.
- **Production:** Follow the [TENANT_MIGRATION_STRATEGY.md](TENANT_MIGRATION_STRATEGY.md) to apply additive DDL changes incrementally.
