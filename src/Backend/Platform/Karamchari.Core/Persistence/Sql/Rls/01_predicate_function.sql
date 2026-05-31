-- =============================================================================
-- Karamchari RLS bootstrap, step 1: tenant access predicate function.
--
-- Single shared predicate, parameterized by the row's TenantId. The session
-- value is set by RlsSessionContextInterceptor on every connection open.
--
-- Behaviour:
--   - Returns 1 row only when @TenantId equals SESSION_CONTEXT('TenantId').
--   - When SESSION_CONTEXT is not set, the comparison evaluates to UNKNOWN
--     and the row is filtered out (fail-closed).
--   - There is intentionally NO admin / db_owner escape hatch here. Cross-tenant
--     work (e.g. the outbox relay) MUST iterate tenants and set
--     SESSION_CONTEXT for each tenant in turn.
--
-- WITH SCHEMABINDING is required for the function to be usable as a security
-- policy predicate.
--
-- IDEMPOTENCY: This function is referenced by every per-tenant security policy.
-- Once any policy binds to it the function CANNOT be dropped or altered while
-- referenced (SQL error 3729). It is therefore created ONLY IF MISSING and is
-- never dropped here. Its definition is stable; if it ever must change, the
-- dependent per-tenant policies are dropped first by 02_tenant_policy. This
-- makes the bootstrap safe to re-run on every deploy.
-- =============================================================================

IF OBJECT_ID(N'[security].[fn_tenant_access]', N'IF') IS NULL
BEGIN
    EXEC(N'
        CREATE FUNCTION [security].[fn_tenant_access](@TenantId NVARCHAR(64))
        RETURNS TABLE
        WITH SCHEMABINDING
        AS
        RETURN
            SELECT 1 AS fn_result
            WHERE @TenantId IS NOT NULL
              AND @TenantId = CAST(SESSION_CONTEXT(N''TenantId'') AS NVARCHAR(64));
    ');
END
GO
