# TENANT MIGRATION STRATEGY

**Date:** 2026-05-30
**Scope:** Staging & Production Schema Evolution

## 1. Core Principle
The platform follows an **Additive-Only** migration strategy for production and staging environments. We prioritize data preservation and platform stability over ease of remediation.

**Explicit Restriction:** The platform must **NEVER** drop tenant schemas, recreate tenant schemas, or destroy existing tenant data for the purpose of architectural remediation or schema synchronization.

## 2. Migration Workflow

1. **Discover Missing Artifacts:**
   - Execute `ITenantModelDiscoveryService` to identify the expected set of relational artifacts (entities, owned collections, join tables) from the current EF Core model.
   - Compare with the existing physical artifacts in the target tenant schema.

2. **Generate Additive DDL:**
   - For every missing artifact, generate a `SELECT * INTO [tenant_X].[Table] FROM [dbo].[Table] WHERE 1=0` or equivalent additive script.
   - Ensure the script is idempotent (`IF NOT EXISTS...`).

3. **Apply Missing Artifacts:**
   - Execute the additive DDL against the target tenant schema.
   - Note: Data from `dbo` (template) is cloned for new table structures; existing tables are never modified.

4. **Verify Equality:**
   - Re-run Artifact Set Verification (ASV) to ensure the physical schema now matches the source model metadata across all 175+ artifacts.

5. **Certify:**
   - Transition tenant status from `Quarantined` to `Active` (if applicable).
   - Log the migration event in the platform audit trail.

## 3. Local Development Exception
For local development and individual developer environments, automated wipe-and-reprovisioning (via `setup-local.sh` or `--provision-dev-tenants`) is the preferred mechanism for speed and consistency. This exception does **not** apply to shared staging or production environments.
