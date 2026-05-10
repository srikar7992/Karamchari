# Karamchari Golden Path Reference Guide

This document defines the strictly enforced, canonical implementation strategies for all core platform operations. Any deviation from these paths will fail automated architectural fitness checks.

## 1. Consumer Execution
**Golden Path**: Every MassTransit consumer must execute within the `TenantConsumeFilter` pipeline. The filter automatically extracts the `TenantExecutionEnvelope` and calls `TenantExecutionContext.Establish()`. 
*   **Forbidden**: Reading headers manually, calling `AsyncLocal` directly, or executing DbContext queries without the established scope.

## 2. Background Jobs
**Golden Path**: Hangfire/Quartz jobs MUST accept a serialized `TenantJobContextPayload`. They must use `TenantJobExecutionScope.FromSerialized(...)` to rehydrate the context before any business logic executes.
*   **Forbidden**: Relying on ambient thread state, or passing raw Tenant IDs as job parameters.

## 3. SQL Access
**Golden Path**: All domain data access MUST flow through `KaramchariDbContext` which automatically registers the `TenantQueryValidationInterceptor` and global query filters. 
*   **Forbidden**: `IgnoreQueryFilters()`, raw `DbConnection` usage, manual `EXEC sp_set_session_context` calls. 

## 4. Cache Access
**Golden Path**: All distributed caching MUST use the `TenantCacheGuard` and `TenantCacheKeyBuilder`. 
*   **Forbidden**: Direct calls to `IDistributedCache` or `ConnectionMultiplexer` without tenant prefixes.

## 5. Event Publishing
**Golden Path**: Use `IIntegrationEvent` wrapped in a `TenantExecutionEnvelope`. `TenantPublishFilter` automatically handles this.
*   **Forbidden**: Publishing bare C# records without inheriting from `IIntegrationEvent`.

## 6. Tenant Provisioning
**Golden Path**: Handled exclusively by `TenantProvisioningService` which manages the RLS Bootstrap, Schema Creation, and Seeding as an atomic transaction.
*   **Forbidden**: Manual SQL schema creation or modifying the `dbo` namespace directly.

## Enforcement
These golden paths are statically verified via `PlatformFitnessTests` (ArchUnitNET/Roslyn) during CI/CD.
