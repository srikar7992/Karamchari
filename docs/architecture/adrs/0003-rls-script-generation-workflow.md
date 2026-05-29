# ADR 0003 — RLS script generation workflow

- **Status:** Accepted
- **Date:** 2026-05-02
- **Deciders:** Solo founder
- **Related:** ADR 0001 (Multi-tenancy), ADR 0002 (Row-Level Security)

## Context

ADR 0001 chose tenant schemas plus SQL Server Row-Level Security (RLS) as a defense-in-depth boundary. ADR 0002 specified the security model: a shared predicate function in the `security` schema, FILTER + four BLOCK predicates per tenant table, no admin escape.

That gives us two jobs whenever a bounded context adds tenant-owned data:

- EF must map the table with a `TenantId` column so application saves can stamp and validate tenant ownership.
- Tenant provisioning must create RLS policies for the table so SQL Server enforces the same boundary even if application code issues a bad query.

The risky part is drift. If a bounded context adds a table but provisioning forgets to add RLS, the database safety net silently weakens.

## Decision

RLS policy generation is driven by code-owned table registration, not handwritten per-tenant SQL.

Each bounded context registers every tenant-owned table during DI startup with `RegisterTenantTable`. The shared `ITenantTableRegistry` is the single source of truth consumed by `RlsScriptGenerator`.

The provisioning workflow is:

1. API startup calls `AddKaramchariCore`, then each bounded context extension such as `AddKaramchariHR`.
2. Each bounded context calls `RegisterTenantTable` for its tenant-owned tables. HR registers `Employees`. (MassTransit's outbox tables are explicitly **not** registered — per ADR 0002 they live in `dbo` as shared infrastructure.)
3. Tenant provisioning creates the tenant schema and applies EF migrations or generated DDL so the tables exist under that schema.
4. Provisioning calls `RlsScriptGenerator.BuildBootstrapScripts()` once per database to create the `security` schema and shared predicate function.
5. Provisioning calls `BuildTenantPolicyScript(tenant)` after the tenant tables exist. The generator emits one tenant policy covering every registered table.
6. Runtime EF connections set `SESSION_CONTEXT(N'TenantId', @id)` through `RlsSessionContextInterceptor`; SQL Server evaluates the generated FILTER and BLOCK predicates against that value.

The generator validates tenant ids, schema names, table names, and tenant column names before emitting DDL. Templates stay embedded in `Karamchari.Core` so SQL is reviewed source code, not a runtime string assembled by an LLM or request payload.

## Consequences

**Positive**

- Adding a tenant-owned table has an explicit startup step: register it or provisioning cannot cover it.
- The same registry can be inspected in tests to catch missing RLS coverage before deployment.
- Bootstrap SQL remains idempotent and shared across tenants; tenant policy SQL is generated only after the schema and tables exist.

**Negative / accepted risks**

- Developers must remember to register new tenant-owned tables in the bounded-context DI extension. Mitigation: add architecture tests that compare EF `ITenantOwned` entity tables with `ITenantTableRegistry`.
- Renaming a table requires coordinating EF migration history and policy regeneration. Mitigation: treat RLS policy updates as part of the same tenant provisioning release step.
- The generator emits SQL Server-specific DDL. This is acceptable because ADR 0001 already commits persistence to Azure SQL.
