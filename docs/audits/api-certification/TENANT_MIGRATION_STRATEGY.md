# TENANT MIGRATION STRATEGY

**Date:** 2026-05-30
**Status:** ADOPTED

## Explicit Restriction

The platform **must never**:

- Drop a tenant schema
- Recreate a tenant schema
- Truncate tenant tables
- Destroy existing tenant data

for any remediation, migration, or re-provisioning purpose.

This restriction is absolute. It applies in all environments including Local.

---

## Strategy by Environment

### Local / Development

Reprovisioning is permitted **only** when explicitly invoked by a developer via `--provision-dev-tenants`.

This flag:
1. Checks whether the tenant schema exists.
2. If it does not exist — creates it from the EF Core model (full provision).
3. If it exists — performs an **additive reconciliation** (see below). It does **not** drop or recreate.
4. Executes ASV (Artifact Set Verification) at the end to certify equality.

---

### Staging / Production

Reprovisioning is **never** permitted. Only additive migration is allowed.

---

## Additive Migration Flow

When drift is detected (artifacts missing from a tenant schema):

```
1. Discover Missing Artifacts
   ↓  TenantProvisioningService.DiscoverMissingArtifactsAsync()
   
2. Generate DDL
   ↓  RlsScriptGenerator.GenerateAdditiveMigrationScript()
   
3. Apply Missing Artifacts
   ↓  Execute DDL inside a transaction, scoped to the tenant schema
   
4. Verify Equality
   ↓  TenantProvisioningService.VerifyArtifactEqualityAsync()
   
5. Certify
   ↓  Mark tenant as HEALTHY in the platform registry
```

---

## Required Properties

| Property | Value |
|---|---|
| **Additive only** | DDL generated contains only `ALTER TABLE ADD COLUMN`, `CREATE TABLE`, `CREATE INDEX`. Never `DROP`. |
| **Transactional** | All DDL for a single tenant is applied in a single SQL transaction. A partial migration fails atomically. |
| **Idempotent** | Each artifact is checked for existence before creation (`IF NOT EXISTS`). |
| **Isolated** | DDL is scoped to the specific tenant schema. No cross-tenant mutations. |
| **Audited** | Every migration event is written to the `platform.TenantMigrationAuditLog` table. |

---

## Rollback

Because migration is **additive only**, rollback is not applicable in the traditional sense. New columns and tables that are added do not break existing queries. If a migration introduces a functional regression:

1. A code rollback is preferred (remove the EF model change, redeploy).
2. The physical column or table remains in the tenant schema. It will be treated as a P3 warning (extra artifact).
3. Manual DDL cleanup is only performed by a human operator with explicit change approval.

---

## Implementation Status

| Component | Status |
|---|---|
| `TenantProvisioningService.ProvisionAsync` | ✅ Implemented (full provision for new tenants) |
| `TenantProvisioningService.ReconcileAsync` | 🔲 Scheduled (additive reconciliation for existing tenants) |
| `RlsScriptGenerator.GenerateAdditiveMigrationScript` | 🔲 Scheduled |
| `TenantMigrationAuditLog` persistence | 🔲 Scheduled |
| `--reconcile-tenants` CLI flag | 🔲 Scheduled (separate from `--provision-dev-tenants`) |

Additive reconciliation is a platform operations capability scheduled for the Platform Ops sprint. Until then, production schema changes are applied via standard EF Core migrations (which already generate safe `IF NOT EXISTS`-style SQL for new tables and columns).
