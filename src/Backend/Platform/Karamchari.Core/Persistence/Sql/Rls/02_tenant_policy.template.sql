-- =============================================================================
-- Karamchari RLS, step 2: per-tenant security policy.
--
-- Template. Tokens substituted by RlsScriptGenerator at provisioning time:
--   {{PolicyName}}    e.g.  TenantPolicy_acme
--   {{SchemaName}}    e.g.  tenant_acme
--   TableList         repeated FILTER + BLOCK ADD lines, generated per registered table
--
-- A policy holds many predicates and is created/altered atomically. We emit:
--   FILTER PREDICATE   — protects SELECT (visible rows must match TenantId)
--   BLOCK  PREDICATE   AFTER  INSERT  — INSERT'd row must match TenantId
--   BLOCK  PREDICATE   BEFORE UPDATE  — current row's TenantId must match
--   BLOCK  PREDICATE   AFTER  UPDATE  — updated row's TenantId must still match
--   BLOCK  PREDICATE   BEFORE DELETE  — current row's TenantId must match
--
-- The result: even raw ADO.NET that bypasses the schema interceptor cannot
-- read or write data belonging to another tenant.
--
-- Idempotent: drops the policy if it already exists, then recreates it.
-- =============================================================================

IF EXISTS (
    SELECT 1
    FROM sys.security_policies
    WHERE name = N'{{PolicyName}}' AND schema_id = SCHEMA_ID(N'security')
)
BEGIN
    EXEC(N'DROP SECURITY POLICY [security].[{{PolicyName}}];');
END
GO

CREATE SECURITY POLICY [security].[{{PolicyName}}]
WITH (STATE = ON, SCHEMABINDING = ON);
GO

{{TableList}}
