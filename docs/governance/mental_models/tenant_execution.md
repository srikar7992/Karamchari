# Mental Model: Tenant Execution Flow

## Purpose
To ensure that every operation in the Karamchari platform occurs within a strictly bounded, isolated tenant context. 

## The Core Concept: `TenantExecutionContext`
Instead of passing `tenantId` strings through thousands of methods, the platform uses an ambient, immutable **Execution Scope**.

Think of it like a diving bell. Once you are inside the `TenantExecutionContext`, all air (database connections, caches, logs) comes from that bell. You cannot breathe another tenant's air.

## The Lifecycle

1.  **Entry Point (The Air Lock)**: A request enters via HTTP, RabbitMQ, or Hangfire.
2.  **Extraction**: The infrastructure (Middleware/Filter) extracts the `TenantExecutionEnvelope` (contains Tenant ID, Correlation ID).
3.  **Establishment**: The infrastructure calls `TenantExecutionContext.Establish()`. This locks the current async flow to the tenant.
4.  **Execution**: Business logic runs. It never sees the Tenant ID.
5.  **Data Access (The Hose)**: When business logic queries EF Core, the `TenantQueryValidationInterceptor` automatically reads the ambient Context and applies it to SQL Server via `sp_set_session_context`.
6.  **Exit**: The scope is disposed, and the thread is scrubbed clean.

## Debugging Strategy
*   **Missing Context?** If an operation fails with `TenantResolutionException`, it means the business logic escaped the diving bell (e.g., an unawaited Task or a direct `Task.Run` without capturing the context).
*   **Tracing**: Search Seq for `karamchari.tenant.id` or `karamchari.correlation.id`. The entire lifecycle is linked automatically.

## Invariants
*   **Immutable**: You cannot change tenants mid-execution.
*   **Required**: No database operation can succeed without an established context.
