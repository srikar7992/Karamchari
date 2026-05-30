# PLATFORM SOURCE OF TRUTH REPORT

**Date:** 2026-05-30
**Status:** ✅ GOVERNED

## Subsystem Governance Matrix

| Subsystem | Source of Truth | Discovery Mechanism | Drift Detection | Certification Method |
|---|---|---|---|---|
| **Provisioning (GAP-1)** | EF Core Model Metadata | `ITenantModelDiscoveryService` | Startup Artifact Set Verification (ASV) | `TENANT_PROVISIONING_CERTIFICATION` |
| **Authorization (GAP-2)** | `Permissions.cs` / `Roles.cs` | Reflection-based Scanning | `PermissionCoverageTests` / `RoleCoverageTests` | `AUTHORIZATION_CERTIFICATION` |
| **Tenant Isolation** | JWT `tenant_id` Claim | `TenantSchemaCommandInterceptor` | RLS Session Context Failsafe | `TENANT_ISOLATION_CERTIFICATION` |
| **API Surface** | Minimal APIs Metadata | ASP.NET Core OpenAPI (Scalar) | PR-based OpenAPI Diff | `SCALAR_CERTIFICATION` |
| **Async Messaging** | Execution Context Interceptors | MassTransit Send/Publish Filters | Transactional Outbox Proof | `ASYNC_CERTIFICATION` |

## Architectural Debt Findings
- **Legacy Registry:** `ITenantTableRegistry` has been replaced by `ITenantModelDiscoveryService`. All manual table registrations have been deprecated.
- **Session Context:** Redundant calls to `sp_set_session_context` exist in `RetrySafeSessionReset` and `RlsConnectionGuard`. These should be consolidated into the `RlsSessionContextInterceptor` in a future refactor.
- **Redundant Interceptors:** The platform has multiple overlapping connection interceptors. Consolidation is recommended to improve performance and simplify debugging.

## Certification Rule
A subsystem is considered governed only when its discovery mechanism is automated and its drift detection is part of the continuous build/startup cycle.
- **GAP-1:** Governed.
- **GAP-2:** Governed (Tests implemented).
- **Isolation:** Governed.
