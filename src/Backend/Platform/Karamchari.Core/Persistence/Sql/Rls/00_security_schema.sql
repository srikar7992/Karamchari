-- =============================================================================
-- Karamchari RLS bootstrap, step 0: shared 'security' schema.
--
-- Runs ONCE per database. Owns the predicate function and the per-tenant
-- security policies. Lives outside any tenant schema because it must be visible
-- to every tenant table without being subject to schema rewriting.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'security')
BEGIN
    EXEC(N'CREATE SCHEMA [security] AUTHORIZATION [dbo];');
END
