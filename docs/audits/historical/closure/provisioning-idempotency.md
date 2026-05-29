# WS2 — Provisioning Idempotency

**Status: ✅ CLOSED (was CRITICAL).** Bootstrap is fully idempotent; exit code 0 every run.

## Root cause
`01_predicate_function.sql` did `DROP FUNCTION [security].[fn_tenant_access]` then `CREATE`. On re-run the function is referenced by the schema-bound `TenantPolicy_*` policies → SQL error **3729** → unhandled exception → process **exit 134 (SIGABRT)**.

## Changes implemented
1. **`01_predicate_function.sql`**: now `IF OBJECT_ID(...) IS NULL EXEC('CREATE FUNCTION ...')` — create-only-if-missing, never dropped while referenced.
2. **Deterministic exit**: provisioning block in `Program.cs` wrapped in try/catch with `Serilog.Log.CloseAndFlushAsync()` + `Environment.Exit(0)` on success / `Environment.Exit(1)` on failure (no more host-disposal abort).
3. Schema create, table clone, and per-tenant policy (drop+recreate) were already `IF (NOT) EXISTS`.

## Verification — 10 consecutive runs
`docs/closure/idempotency-matrix.csv`:
```
RUN,EXIT,SCHEMAS,POLICIES,PREDICATES,TABLES
1,0,3,3,1680,600
2,0,3,3,1680,600
...
10,0,3,3,1680,600
```
- **Exit 0 on all 10 runs.**
- **0 duplicate objects**: schemas (3), policies (3), predicates (1680), tables (600) perfectly stable.
- **0 orphaned objects.**

## Verdict
Provisioning Idempotency = **PASS**.
